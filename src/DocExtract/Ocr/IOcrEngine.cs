namespace DocExtract.Ocr;

/// <summary>
/// Точка расширения для локальных и облачных OCR-движков.
/// </summary>
public interface IOcrEngine : IAsyncDisposable, IDisposable
{
    string Name { get; }

    Task<PageOcrResult> RecognizeAsync(OcrImage image, OcrRequest request, CancellationToken cancellationToken = default);
}

public sealed class OcrRequest
{
    public IReadOnlyList<string> Languages { get; init; } = ["ru", "en"];
}

/// <summary>
/// Входной растр для OCR (PNG в памяти + размеры).
/// </summary>
public sealed class OcrImage
{
    public required byte[] PngBytes { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }
}

public sealed class PageOcrResult
{
    public required string Text { get; init; }

    public IReadOnlyList<OcrLine> Lines { get; init; } = [];
}

public sealed class OcrLine
{
    public required string Text { get; init; }

    public float Confidence { get; init; }

    public PixelBox BoundingBox { get; init; }

    public IReadOnlyList<OcrWord> Words { get; init; } = [];
}

public sealed class OcrWord
{
    public required string Text { get; init; }

    public float Confidence { get; init; }

    public PixelBox BoundingBox { get; init; }
}
