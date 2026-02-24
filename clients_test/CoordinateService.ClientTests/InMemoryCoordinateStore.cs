using System.Collections.Concurrent;
using CoordinateService.Models.Domain;
using CoordinateService.Models.Response;
using CoordinateService.Services;

namespace CoordinateService.ClientTests;

public class InMemoryCoordinateStore : ICoordinateStore
{
    private readonly ConcurrentDictionary<Guid, CoordinateSystem> _systems = new();
    private readonly ConcurrentDictionary<Guid, (Guid SystemId, int X, int Y, Direction Dir, DateTime CreatedAt)> _points = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task<CoordinateSystem> CreateSystemAsync(string name, int width, int height)
    {
        var sys = new CoordinateSystem(Guid.NewGuid(), name, width, height, DateTime.UtcNow);
        _systems[sys.Id] = sys;
        return Task.FromResult(sys);
    }

    public Task<CoordinateSystemDetail?> GetSystemAsync(Guid id)
    {
        if (!_systems.TryGetValue(id, out var sys)) return Task.FromResult<CoordinateSystemDetail?>(null);
        var point = _points.Values
            .Where(p => p.SystemId == id)
            .Select(p => _points.First(kv => kv.Value.SystemId == id))
            .Select(kv => new PointDetail(kv.Key, kv.Value.X, kv.Value.Y, kv.Value.Dir, kv.Value.CreatedAt))
            .FirstOrDefault();
        return Task.FromResult<CoordinateSystemDetail?>(
            new CoordinateSystemDetail(sys.Id, sys.Name, sys.Width, sys.Height, sys.CreatedAt, point));
    }

    public Task<CoordinateSystem?> GetSystemMetadataAsync(Guid systemId)
    {
        _systems.TryGetValue(systemId, out var sys);
        return Task.FromResult(sys);
    }

    public Task<bool> SystemExistsAsync(Guid id) => Task.FromResult(_systems.ContainsKey(id));

    public Task<bool> SystemHasPointAsync(Guid systemId) =>
        Task.FromResult(_points.Values.Any(p => p.SystemId == systemId));

    public Task<bool> DeleteSystemAsync(Guid id)
    {
        if (!_systems.TryRemove(id, out _)) return Task.FromResult(false);
        var pointsToRemove = _points.Where(kv => kv.Value.SystemId == id).Select(kv => kv.Key).ToList();
        foreach (var pid in pointsToRemove) _points.TryRemove(pid, out _);
        return Task.FromResult(true);
    }

    public Task<CreatePointResponse> CreatePointAsync(Guid systemId, int x, int y, Direction direction)
    {
        var id = Guid.NewGuid();
        _points[id] = (systemId, x, y, direction, DateTime.UtcNow);
        return Task.FromResult(new CreatePointResponse(id, systemId, x, y, direction));
    }

    public Task<PointDetail?> GetPointAsync(Guid id)
    {
        if (!_points.TryGetValue(id, out var p)) return Task.FromResult<PointDetail?>(null);
        return Task.FromResult<PointDetail?>(new PointDetail(id, p.X, p.Y, p.Dir, p.CreatedAt));
    }

    public Task<Guid?> GetPointSystemIdAsync(Guid pointId)
    {
        if (!_points.TryGetValue(pointId, out var p)) return Task.FromResult<Guid?>(null);
        return Task.FromResult<Guid?>(p.SystemId);
    }

    public Task<bool> DeletePointAsync(Guid id) => Task.FromResult(_points.TryRemove(id, out _));

    public Task<MovePointResponse> UpdatePointAsync(Guid id, int x, int y, Direction direction)
    {
        var old = _points[id];
        _points[id] = (old.SystemId, x, y, direction, old.CreatedAt);
        return Task.FromResult(new MovePointResponse(id, x, y, direction, old.SystemId));
    }

    public Task<bool> IsHealthyAsync() => Task.FromResult(true);
}
