using CoordinateService.Models.Domain;

namespace CoordinateService.Models.Response;

public record MovePointResponse(Guid Id, int X, int Y, Direction Direction, Guid SystemId);
