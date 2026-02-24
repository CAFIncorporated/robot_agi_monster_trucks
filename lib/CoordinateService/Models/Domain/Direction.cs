using System.Text.Json.Serialization;

namespace CoordinateService.Models.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Direction { N, S, E, W }
