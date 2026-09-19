namespace DocExtract;

/// <summary>
/// Слово с геометрией.
/// </summary>
public sealed class TextWord
{
    public required string Text { get; init; }

    public float Confidence { get; init; }

    public PixelBox BoundingBox { get; init; }

    public Box NormalizedBox { get; init; }
}

/// <summary>
/// Строка: набор слов.
/// </summary>
public sealed class TextLine
{
    public required string Text { get; init; }

    public float Confidence { get; init; }

    public PixelBox BoundingBox { get; init; }

    public Box NormalizedBox { get; init; }

    public IReadOnlyList<TextWord> Words { get; init; } = [];
}

/// <summary>
/// Блок (абзац или группа строк).
/// </summary>
public sealed class TextBlock
{
    public required string Text { get; init; }

    public float Confidence { get; init; }

    public PixelBox BoundingBox { get; init; }

    public Box NormalizedBox { get; init; }

    public IReadOnlyList<TextLine> Lines { get; init; } = [];
}
