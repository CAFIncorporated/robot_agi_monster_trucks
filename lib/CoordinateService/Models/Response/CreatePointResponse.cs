using CoordinateService.Models.Domain;

namespace CoordinateService.Models.Response;

public record CreatePointResponse(Guid Id, Guid SystemId, int X, int Y, Direction Direction);
