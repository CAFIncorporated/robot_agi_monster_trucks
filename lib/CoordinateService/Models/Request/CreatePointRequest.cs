using CoordinateService.Models.Domain;

namespace CoordinateService.Models.Request;

public record CreatePointRequest(int X, int Y, Direction Direction);
