namespace CoordinateService.Models.Domain;

public record CoordinateSystem(Guid Id, string Name, int Width, int Height, DateTime CreatedAt);
