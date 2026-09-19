namespace DocExtract;

/// <summary>
/// Зона на конкретной странице в нормализованных координатах.
/// </summary>
public sealed class PageRegion
{
    public PageRegion()
    {
    }

    public PageRegion(int pageIndex, Box normalizedBounds)
    {
        if (pageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        PageIndex = pageIndex;
        NormalizedBounds = normalizedBounds.Clamp();
        if (NormalizedBounds.IsEmpty)
        {
            throw new ArgumentException("Зона страницы не должна быть пустой.", nameof(normalizedBounds));
        }
    }

    public int PageIndex { get; init; }

    public Box NormalizedBounds { get; init; }
}
