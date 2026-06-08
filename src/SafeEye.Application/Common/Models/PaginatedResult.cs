namespace SafeEye.Application.Common.Models;

public record PaginatedResult<T>(IReadOnlyList<T> Items, int Total, int Limit);