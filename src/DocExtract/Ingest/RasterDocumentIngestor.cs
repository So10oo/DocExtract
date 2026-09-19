namespace DocExtract.Ingest;

internal sealed class RasterDocumentIngestor : IDocumentIngestor
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff" };

    public bool CanHandle(DocumentSource source) => Extensions.Contains(source.Extension);

    public Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken) =>
        Task.FromResult(1);

    public Task<IngestedDocument> IngestAsync(DocumentSource source, ExtractorOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = source.ReadAllBytes();
        var (w, h, png) = RasterCodec.ToPng(bytes);
        var page = new IngestedPage
        {
            PageIndex = 0,
            PixelWidth = w,
            PixelHeight = h,
            Dpi = options.RenderDpi,
            Kind = PageSourceKind.Ocr,
            RasterPng = png
        };
        return Task.FromResult(new IngestedDocument
        {
            FileName = source.FileName,
            FullPath = source.FilePath,
            Pages = [page]
        });
    }

    public Task<PageImage> RenderPageAsync(DocumentSource source, int pageIndex, ExtractorOptions options, CancellationToken cancellationToken)
    {
        if (pageIndex != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        var bytes = source.ReadAllBytes();
        var (w, h, png) = RasterCodec.ToPng(bytes);
        return Task.FromResult(new PageImage
        {
            PageIndex = 0,
            Width = w,
            Height = h,
            Dpi = options.RenderDpi,
            PngBytes = png
        });
    }
}
