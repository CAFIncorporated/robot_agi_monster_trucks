using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using CoordinateService.Models.Domain;
using CoordinateService.Models.Request;
using CoordinateService.Models.Response;
using CoordinateService.Services;

namespace CoordinateService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/points")]
public class PointsController : ControllerBase
{
    private readonly ICoordinateStore _store;
    private readonly ICacheService _cache;

    public PointsController(ICoordinateStore store, ICacheService cache)
    {
        _store = store;
        _cache = cache;
    }

    [HttpGet("{pointId:guid}")]
    [ProducesResponseType(typeof(PointDetail), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PointDetail>> Get(Guid pointId)
    {
        var cacheKey = $"point:{pointId}";
        var cached = _cache.Get<PointDetail>(cacheKey);
        if (cached is not null) return Ok(cached);

        var point = await _store.GetPointAsync(pointId);
        if (point is null) return NotFound();

        _cache.Set(cacheKey, point);
        return Ok(point);
    }

    [HttpDelete("{pointId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid pointId)
    {
        var systemId = await _store.GetPointSystemIdAsync(pointId);
        var deleted = await _store.DeletePointAsync(pointId);
        if (!deleted) return NotFound();

        _cache.Evict($"point:{pointId}");
        if (systemId.HasValue) _cache.Evict($"system:{systemId.Value}");
        return NoContent();
    }

    [HttpPost("{pointId:guid}/move")]
    [ProducesResponseType(typeof(MovePointResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MovePointResponse>> Move(Guid pointId, [FromBody] MovePointRequest request)
    {
        if (request.Commands is null || request.Commands.Count == 0)
            return BadRequest(new ErrorResponse("Validation failed", "At least one command is required (M, R, L)"));

        var commands = new List<MoveCommand>();
        foreach (var c in request.Commands)
        {
            var s = (c ?? "").Trim().ToUpperInvariant();
            if (s.Length != 1 || (s[0] != 'M' && s[0] != 'R' && s[0] != 'L'))
                return BadRequest(new ErrorResponse("Invalid command", $"'{c}' is not a valid command (M=move, R=right, L=left)"));
            commands.Add(s[0] == 'M' ? MoveCommand.M : s[0] == 'R' ? MoveCommand.R : MoveCommand.L);
        }

        var point = await _store.GetPointAsync(pointId);
        if (point is null)
            return NotFound(new ErrorResponse("Not found", "Point does not exist"));

        var systemId = await _store.GetPointSystemIdAsync(pointId);
        if (systemId is null)
            return NotFound(new ErrorResponse("Not found", "Point has no associated system"));

        var system = await _store.GetSystemMetadataAsync(systemId.Value);
        if (system is null)
            return NotFound(new ErrorResponse("Not found", "Coordinate system does not exist"));

        int x = point.X, y = point.Y;
        var dir = point.Direction;

        for (int i = 0; i < commands.Count; i++)
        {
            switch (commands[i])
            {
                case MoveCommand.M:
                    switch (dir)
                    {
                        case Direction.N: y--; break;
                        case Direction.S: y++; break;
                        case Direction.E: x++; break;
                        case Direction.W: x--; break;
                    }
                    if (x < 0 || x >= system.Width || y < 0 || y >= system.Height)
                        return BadRequest(new ErrorResponse("Out of bounds",
                            $"Move {i + 1} (M) results in ({x},{y}) which is outside {system.Width}x{system.Height} system"));
                    break;
                case MoveCommand.R:
                    dir = dir switch { Direction.N => Direction.E, Direction.E => Direction.S, Direction.S => Direction.W, Direction.W => Direction.N, _ => dir };
                    break;
                case MoveCommand.L:
                    dir = dir switch { Direction.N => Direction.W, Direction.W => Direction.S, Direction.S => Direction.E, Direction.E => Direction.N, _ => dir };
                    break;
            }
        }

        var result = await _store.UpdatePointAsync(pointId, x, y, dir);

        _cache.Evict($"point:{pointId}");
        _cache.Evict($"system:{systemId.Value}");
        return Ok(result);
    }
}
