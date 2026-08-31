namespace HcbeApi.Helpers;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems) =>
        new(items, page, pageSize, totalItems, Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize)));
}

public static class Pagination
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 5, 100);
        return (safePage, safePageSize);
    }
}
