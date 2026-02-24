using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using CoordinateService.Models.Request;
using CoordinateService.Models.Response;
using CoordinateService.Services;

namespace CoordinateService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/coordinate-systems")]
public class CoordinateSystemsController : ControllerBase
{
    private readonly ICoordinateStore _store;
    private readonly ICacheService _cache;

    public CoordinateSystemsController(ICoordinateStore store, ICacheService cache)
    {
        _store = store;
        _cache = cache;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateCoordinateSystemResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<CreateCoordinateSystemResponse>> Create([FromBody] CreateCoordinateSystemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("Validation failed", "Name is required"));
        if (request.Width <= 0 || request.Height <= 0)
            return BadRequest(new ErrorResponse("Validation failed", "Width and height must be positive integers"));

        var system = await _store.CreateSystemAsync(request.Name, request.Width, request.Height);
        return Ok(new CreateCoordinateSystemResponse(system.Id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CoordinateSystemDetail), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CoordinateSystemDetail>> Get(Guid id)
    {
        var cacheKey = $"system:{id}";
        var cached = _cache.Get<CoordinateSystemDetail>(cacheKey);
        if (cached is not null) return Ok(cached);

        var system = await _store.GetSystemAsync(id);
        if (system is null) return NotFound();

        _cache.Set(cacheKey, system);
        return Ok(system);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _store.DeleteSystemAsync(id);
        if (!deleted) return NotFound();

        _cache.EvictByPrefix($"system:{id}");
        _cache.EvictByPrefix("point:");
        return NoContent();
    }

    [HttpPost("{systemId:guid}/points")]
    [ProducesResponseType(typeof(CreatePointResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CreatePointResponse>> CreatePoint(Guid systemId, [FromBody] CreatePointRequest request)
    {
        var system = await _store.GetSystemMetadataAsync(systemId);
        if (system is null)
            return NotFound(new ErrorResponse("Not found", "Coordinate system does not exist"));

        if (request.X < 0 || request.X >= system.Width || request.Y < 0 || request.Y >= system.Height)
            return BadRequest(new ErrorResponse("Out of bounds",
                $"Position ({request.X},{request.Y}) is outside {system.Width}x{system.Height} system"));

        if (!Enum.IsDefined(request.Direction))
            return BadRequest(new ErrorResponse("Invalid direction", "Direction must be N, S, E, or W"));

        if (await _store.SystemHasPointAsync(systemId))
            return BadRequest(new ErrorResponse("Limit reached", "System already has a point (v1 limit: 1 point per system)"));

        var point = await _store.CreatePointAsync(systemId, request.X, request.Y, request.Direction);

        _cache.Evict($"system:{systemId}");
        return Ok(point);
    }
}
