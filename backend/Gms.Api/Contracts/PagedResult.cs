namespace Gms.Api.Contracts;

/// <summary>Standard paged list envelope for every list endpoint.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) => new()
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}

/// <summary>Common paging/sorting query parameters shared by list endpoints.</summary>
public class PagedQuery
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; set => _pageSize = value < 1 ? 20 : (value > MaxPageSize ? MaxPageSize : value); }

    /// <summary>Field to sort by (whitelisted per endpoint). Default: createdAt.</summary>
    public string? SortBy { get; set; }

    /// <summary>"asc" or "desc" (default desc).</summary>
    public string? SortDir { get; set; }

    public bool Descending => !string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase);
    public int Skip => (Page - 1) * PageSize;
}
