using System.Text.Json.Serialization;

namespace SafeEye.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SosStatus { Active, Resolved }