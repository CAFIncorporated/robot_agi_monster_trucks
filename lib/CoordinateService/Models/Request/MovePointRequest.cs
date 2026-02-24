namespace CoordinateService.Models.Request;

/// <summary>Commands: M = move forward, R = turn right, L = turn left.</summary>
public record MovePointRequest(List<string> Commands);
