namespace CoordinateService.Models.Response;

public record CoordinateSystemDetail(Guid Id, string Name, int Width, int Height, DateTime CreatedAt, PointDetail? Point);
