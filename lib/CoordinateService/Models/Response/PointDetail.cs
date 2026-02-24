using CoordinateService.Models.Domain;

namespace CoordinateService.Models.Response;

public record PointDetail(Guid Id, int X, int Y, Direction Direction, DateTime CreatedAt);
