using System.Collections;

namespace TaskManager.Application.Common.Pagination;

public sealed class PagedResult<T>
    : IReadOnlyList<T>
{
    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        Items = items
            ?? throw new ArgumentNullException(
                nameof(items));

        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount / (double)PageSize);

    public int Count =>
        Items.Count;

    public T this[int index] =>
        Items[index];

    public IEnumerator<T> GetEnumerator() =>
        Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
