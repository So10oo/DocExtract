namespace DocExtract.Ingest;

internal sealed class IngestedDocument
{
    public required string FileName { get; init; }

    public string? FullPath { get; init; }

    public required IReadOnlyList<IngestedPage> Pages { get; init; }
}

internal sealed class IngestedPage
{
    public int PageIndex { get; init; }

    public int PixelWidth { get; init; }

    public int PixelHeight { get; init; }

    public int Dpi { get; init; }

    public PageSourceKind Kind { get; init; }

    public byte[]? RasterPng { get; init; }

    public IReadOnlyList<EmbeddedWord> EmbeddedWords { get; init; } = [];
}

internal sealed class EmbeddedWord
{
    public required string Text { get; init; }

    public PixelBox BoundingBox { get; init; }

    public float Confidence { get; init; } = 1f;
}

internal interface IDocumentIngestor
{
    bool CanHandle(DocumentSource source);

    Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken);

    Task<IngestedDocument> IngestAsync(DocumentSource source, ExtractorOptions options, CancellationToken cancellationToken);

    Task<PageImage> RenderPageAsync(DocumentSource source, int pageIndex, ExtractorOptions options, CancellationToken cancellationToken);
}
