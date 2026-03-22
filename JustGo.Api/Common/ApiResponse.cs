namespace JustGo.Api.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ApiResult<T>(bool Success, T? Data, string? Message, IReadOnlyList<string>? Errors);
