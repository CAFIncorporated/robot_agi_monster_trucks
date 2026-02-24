using CoordinateService.Models.Domain;
using CoordinateService.Models.Response;

namespace CoordinateService.Services;

public interface ICoordinateStore
{
    Task InitializeAsync();
    Task<CoordinateSystem> CreateSystemAsync(string name, int width, int height);
    Task<CoordinateSystemDetail?> GetSystemAsync(Guid id);
    Task<bool> DeleteSystemAsync(Guid id);
    Task<CreatePointResponse> CreatePointAsync(Guid systemId, int x, int y, Direction direction);
    Task<PointDetail?> GetPointAsync(Guid id);
    Task<Guid?> GetPointSystemIdAsync(Guid pointId);
    Task<bool> DeletePointAsync(Guid id);
    Task<MovePointResponse> UpdatePointAsync(Guid id, int x, int y, Direction direction);
    Task<bool> SystemExistsAsync(Guid id);
    Task<bool> SystemHasPointAsync(Guid systemId);
    Task<CoordinateSystem?> GetSystemMetadataAsync(Guid systemId);
    Task<bool> IsHealthyAsync();
}
