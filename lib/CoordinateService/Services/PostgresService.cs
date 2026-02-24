using Npgsql;
using Polly;
using Polly.Retry;
using CoordinateService.Models.Domain;
using CoordinateService.Models.Response;

namespace CoordinateService.Services;

public class PostgresCoordinateStore : ICoordinateStore
{
    private readonly string _connectionString;
    private readonly ResiliencePipeline _retry;
    private readonly ILogger<PostgresCoordinateStore> _logger;

    public PostgresCoordinateStore(IConfiguration config, ILogger<PostgresCoordinateStore> logger)
    {
        _connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not configured");
        _logger = logger;

        _retry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(ex => ex.IsTransient),
                OnRetry = args =>
                {
                    logger.LogWarning("Postgres retry {Attempt} after {Delay}ms: {Message}",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await _retry.ExecuteAsync(async token => await conn.OpenAsync(token), ct);
        return conn;
    }

    public async Task InitializeAsync()
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS coordinate_systems (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name TEXT NOT NULL,
                width INTEGER NOT NULL CHECK (width > 0),
                height INTEGER NOT NULL CHECK (height > 0),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS points (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                system_id UUID NOT NULL REFERENCES coordinate_systems(id) ON DELETE CASCADE,
                x INTEGER NOT NULL,
                y INTEGER NOT NULL,
                direction TEXT NOT NULL CHECK (direction IN ('N','S','E','W')),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uq_one_point_per_system UNIQUE (system_id)
            );

            CREATE INDEX IF NOT EXISTS idx_points_system_id ON points (system_id);
            """;
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Database initialized");
    }

    public async Task<CoordinateSystem> CreateSystemAsync(string name, int width, int height)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO coordinate_systems (name, width, height)
                VALUES (@name, @width, @height)
                RETURNING id, name, width, height, created_at
                """;
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("width", width);
            cmd.Parameters.AddWithValue("height", height);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            await r.ReadAsync(ct);
            return new CoordinateSystem(r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetDateTime(4));
        }, default);
    }

    public async Task<CoordinateSystemDetail?> GetSystemAsync(Guid id)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT cs.id, cs.name, cs.width, cs.height, cs.created_at,
                       p.id, p.x, p.y, p.direction, p.created_at
                FROM coordinate_systems cs
                LEFT JOIN points p ON p.system_id = cs.id
                WHERE cs.id = @id
                """;
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;

            PointDetail? point = r.IsDBNull(5) ? null :
                new PointDetail(r.GetGuid(5), r.GetInt32(6), r.GetInt32(7), Enum.Parse<Direction>(r.GetString(8)), r.GetDateTime(9));

            return new CoordinateSystemDetail(r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetDateTime(4), point);
        }, default);
    }

    public async Task<CoordinateSystem?> GetSystemMetadataAsync(Guid systemId)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, width, height, created_at FROM coordinate_systems WHERE id = @id";
            cmd.Parameters.AddWithValue("id", systemId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;
            return new CoordinateSystem(r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetDateTime(4));
        }, default);
    }

    public async Task<bool> SystemExistsAsync(Guid id)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM coordinate_systems WHERE id = @id)";
            cmd.Parameters.AddWithValue("id", id);
            return (bool)(await cmd.ExecuteScalarAsync(ct))!;
        }, default);
    }

    public async Task<bool> SystemHasPointAsync(Guid systemId)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM points WHERE system_id = @sid)";
            cmd.Parameters.AddWithValue("sid", systemId);
            return (bool)(await cmd.ExecuteScalarAsync(ct))!;
        }, default);
    }

    public async Task<bool> DeleteSystemAsync(Guid id)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM coordinate_systems WHERE id = @id";
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }, default);
    }

    public async Task<CreatePointResponse> CreatePointAsync(Guid systemId, int x, int y, Direction direction)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO points (system_id, x, y, direction)
                VALUES (@sid, @x, @y, @dir)
                RETURNING id, system_id, x, y, direction
                """;
            cmd.Parameters.AddWithValue("sid", systemId);
            cmd.Parameters.AddWithValue("x", x);
            cmd.Parameters.AddWithValue("y", y);
            cmd.Parameters.AddWithValue("dir", direction.ToString());
            await using var r = await cmd.ExecuteReaderAsync(ct);
            await r.ReadAsync(ct);
            return new CreatePointResponse(r.GetGuid(0), r.GetGuid(1), r.GetInt32(2), r.GetInt32(3), Enum.Parse<Direction>(r.GetString(4)));
        }, default);
    }

    public async Task<PointDetail?> GetPointAsync(Guid id)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, x, y, direction, created_at FROM points WHERE id = @id";
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;
            return new PointDetail(r.GetGuid(0), r.GetInt32(1), r.GetInt32(2), Enum.Parse<Direction>(r.GetString(3)), r.GetDateTime(4));
        }, default);
    }

    public async Task<Guid?> GetPointSystemIdAsync(Guid pointId)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT system_id FROM points WHERE id = @id";
            cmd.Parameters.AddWithValue("id", pointId);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is null ? null : (Guid?)result;
        }, default);
    }

    public async Task<bool> DeletePointAsync(Guid id)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM points WHERE id = @id";
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }, default);
    }

    public async Task<MovePointResponse> UpdatePointAsync(Guid id, int x, int y, Direction direction)
    {
        return await _retry.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE points SET x = @x, y = @y, direction = @dir WHERE id = @id
                RETURNING id, x, y, direction, system_id
                """;
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("x", x);
            cmd.Parameters.AddWithValue("y", y);
            cmd.Parameters.AddWithValue("dir", direction.ToString());
            await using var r = await cmd.ExecuteReaderAsync(ct);
            await r.ReadAsync(ct);
            return new MovePointResponse(r.GetGuid(0), r.GetInt32(1), r.GetInt32(2), Enum.Parse<Direction>(r.GetString(3)), r.GetGuid(4));
        }, default);
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
