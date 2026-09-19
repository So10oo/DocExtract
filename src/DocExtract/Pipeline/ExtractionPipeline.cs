using DocExtract.Ingest;
using DocExtract.Ocr;
using Microsoft.Extensions.Logging;

namespace DocExtract.Pipeline;

internal sealed class ExtractionPipeline
{
    private readonly ExtractorOptions _options;
    private readonly IOcrEngine? _engine;
    private readonly ILogger? _logger;

    public ExtractionPipeline(ExtractorOptions options, IOcrEngine? engine)
    {
        _options = options;
        _engine = engine ?? options.Engine;
        _logger = options.Logger;
    }

    public async Task<DocumentResult> ExtractAsync(DocumentSource source, CancellationToken cancellationToken)
    {
        var ingested = await DocumentIngestorFactory.For(source)
            .IngestAsync(source, _options, cancellationToken)
            .ConfigureAwait(false);

        var pages = await ProcessPagesAsync(ingested.Pages, cancellationToken).ConfigureAwait(false);
        return BuildResult(source, ingested.Pages.Count, pages);
    }

    public async Task<RegionResult> ExtractRegionAsync(DocumentSource source, PageRegion region, CancellationToken cancellationToken)
    {
        var ingested = await DocumentIngestorFactory.For(source)
            .IngestAsync(source, _options, cancellationToken)
            .ConfigureAwait(false);

        if (region.PageIndex >= ingested.Pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Индекс страницы вне документа.");
        }

        var page = ingested.Pages[region.PageIndex];
        var crop = CoordinateMapper.ToPixel(region.NormalizedBounds, page.PixelWidth, page.PixelHeight);
        var cropped = CropPage(page, crop);
        var pageResult = await ProcessPageAsync(
                cropped,
                new PixelBox(crop.X, crop.Y, 0, 0),
                page.PixelWidth,
                page.PixelHeight,
                cancellationToken)
            .ConfigureAwait(false);

        pageResult = new PageResult
        {
            PageIndex = page.PageIndex,
            PixelWidth = page.PixelWidth,
            PixelHeight = page.PixelHeight,
            Dpi = page.Dpi,
            Rotation = 0,
            SourceKind = pageResult.SourceKind,
            Text = pageResult.Text,
            Blocks = pageResult.Blocks
        };

        return new RegionResult
        {
            Region = region,
            Page = pageResult,
            Source = new DocumentMetadata
            {
                FileName = source.FileName,
                FullPath = source.FilePath,
                PageCount = ingested.Pages.Count
            }
        };
    }

    public Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken) =>
        DocumentIngestorFactory.For(source).GetPageCountAsync(source, cancellationToken);

    public Task<PageImage> RenderPageAsync(DocumentSource source, int pageIndex, CancellationToken cancellationToken) =>
        DocumentIngestorFactory.For(source).RenderPageAsync(source, pageIndex, _options, cancellationToken);

    private async Task<PageResult[]> ProcessPagesAsync(
        IReadOnlyList<IngestedPage> pages,
        CancellationToken cancellationToken)
    {
        var results = new PageResult[pages.Count];
        var parallelism = Math.Max(1, _options.MaxDegreeOfParallelism);
        using var gate = new SemaphoreSlim(parallelism);
        var tasks = pages.Select(async (page, i) =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                results[i] = await ProcessPageAsync(page, default, page.PixelWidth, page.PixelHeight, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    private async Task<PageResult> ProcessPageAsync(
        IngestedPage page,
        PixelBox originOffset,
        int layoutWidth,
        int layoutHeight,
        CancellationToken cancellationToken)
    {
        if (page.Kind == PageSourceKind.EmbeddedText && page.EmbeddedWords.Count > 0)
        {
            var words = page.EmbeddedWords
                .Select(w => new LayoutWord(w.Text, w.BoundingBox.Offset(originOffset.X, originOffset.Y), w.Confidence))
                .ToArray();
            var blocks = TextLayoutBuilder.FromWords(words, layoutWidth, layoutHeight);
            return ToPageResult(page, PageSourceKind.EmbeddedText, blocks, layoutWidth, layoutHeight);
        }

        if (page.RasterPng is null)
        {
            return ToPageResult(page, PageSourceKind.Empty, [], layoutWidth, layoutHeight);
        }

        if (_engine is null)
        {
            throw new InvalidOperationException(
                "Для OCR нужен IOcrEngine. Подключите DocExtract.Engines.Onnx (OnnxOcrEngine) или передайте свой движок.");
        }

        _logger?.LogInformation("OCR страницы {Page} ({W}x{H})", page.PageIndex, page.PixelWidth, page.PixelHeight);
        var ocr = await _engine.RecognizeAsync(
                new OcrImage { PngBytes = page.RasterPng, Width = page.PixelWidth, Height = page.PixelHeight },
                new OcrRequest { Languages = _options.Languages },
                cancellationToken)
            .ConfigureAwait(false);

        var ocrBlocks = TextLayoutBuilder.FromOcrLines(ocr.Lines, layoutWidth, layoutHeight, originOffset.X, originOffset.Y);
        return ToPageResult(page, PageSourceKind.Ocr, ocrBlocks, layoutWidth, layoutHeight, ocr.Text);
    }

    private static IngestedPage CropPage(IngestedPage page, PixelBox crop)
    {
        var words = page.EmbeddedWords
            .Where(w => !w.BoundingBox.Intersect(crop).IsEmpty)
            .Select(w => new EmbeddedWord
            {
                Text = w.Text,
                Confidence = w.Confidence,
                BoundingBox = new PixelBox(
                    w.BoundingBox.X - crop.X,
                    w.BoundingBox.Y - crop.Y,
                    w.BoundingBox.Width,
                    w.BoundingBox.Height)
            })
            .ToArray();

        byte[]? png = page.RasterPng is null ? null : RasterCodec.CropPng(page.RasterPng, crop);
        var width = Math.Max(1, (int)Math.Round(crop.Width));
        var height = Math.Max(1, (int)Math.Round(crop.Height));

        return new IngestedPage
        {
            PageIndex = page.PageIndex,
            PixelWidth = width,
            PixelHeight = height,
            Dpi = page.Dpi,
            Kind = page.Kind == PageSourceKind.EmbeddedText && words.Length > 0
                ? PageSourceKind.EmbeddedText
                : PageSourceKind.Ocr,
            RasterPng = png,
            EmbeddedWords = words
        };
    }

    private static PageResult ToPageResult(
        IngestedPage page,
        PageSourceKind kind,
        IReadOnlyList<TextBlock> blocks,
        int layoutWidth,
        int layoutHeight,
        string? ocrText = null)
    {
        var text = !string.IsNullOrWhiteSpace(ocrText)
            ? ocrText
            : string.Join(Environment.NewLine, blocks.Select(b => b.Text));
        return new PageResult
        {
            PageIndex = page.PageIndex,
            PixelWidth = layoutWidth,
            PixelHeight = layoutHeight,
            Dpi = page.Dpi,
            Rotation = 0,
            SourceKind = blocks.Count == 0 ? PageSourceKind.Empty : kind,
            Text = text,
            Blocks = blocks
        };
    }

    private DocumentResult BuildResult(DocumentSource source, int pageCount, IReadOnlyList<PageResult> pages) =>
        new()
        {
            Source = new DocumentMetadata
            {
                FileName = source.FileName,
                FullPath = source.FilePath,
                PageCount = pageCount
            },
            Options = new ExtractorOptionsSnapshot
            {
                Languages = _options.Languages,
                PreferEmbeddedText = _options.PreferEmbeddedText,
                Gpu = _options.Gpu,
                RenderDpi = _options.RenderDpi
            },
            Pages = pages
        };
}
