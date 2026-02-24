namespace CoordinateService.Models.Response;

public record ErrorResponse(string Error, string? Detail = null);
