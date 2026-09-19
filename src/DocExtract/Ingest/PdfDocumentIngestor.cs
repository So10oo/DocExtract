using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocExtract.Ingest;

internal sealed class PdfDocumentIngestor : IDocumentIngestor
{
    public bool CanHandle(DocumentSource source) => source.Extension is ".pdf";

    public Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = source.OpenRead();
        using var pdf = PdfDocument.Open(stream);
        return Task.FromResult(pdf.NumberOfPages);
    }

    public async Task<IngestedDocument> IngestAsync(DocumentSource source, ExtractorOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pages = new List<IngestedPage>();
        using (var stream = source.OpenRead())
        using (var pdf = PdfDocument.Open(stream))
        {
            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                pages.Add(BuildPage(source, page, options, rasterize: true));
            }
        }

        return new IngestedDocument
        {
            FileName = source.FileName,
            FullPath = source.FilePath,
            Pages = pages
        };
    }

    public Task<PageImage> RenderPageAsync(DocumentSource source, int pageIndex, ExtractorOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = GetPageCountAsync(source, cancellationToken).GetAwaiter().GetResult();
        if (pageIndex < 0 || pageIndex >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        var (w, h, png) = Rasterize(source, pageIndex, options.RenderDpi);
        return Task.FromResult(new PageImage
        {
            PageIndex = pageIndex,
            Width = w,
            Height = h,
            Dpi = options.RenderDpi,
            PngBytes = png
        });
    }

    internal static IngestedPage BuildPage(DocumentSource source, Page page, ExtractorOptions options, bool rasterize)
    {
        var dpi = options.RenderDpi;
        var (pixelW, pixelH) = CoordinateMapper.PagePixelSize(page.Width, page.Height, dpi);
        var words = ExtractWords(page, dpi, pixelW, pixelH);
        var embeddedLength = words.Sum(w => w.Text.Trim().Length);
        var useText = options.PreferEmbeddedText && embeddedLength >= options.EmbeddedTextThreshold;

        byte[]? png = null;
        if (rasterize && !useText)
        {
            (_, _, png) = Rasterize(source, page.Number - 1, dpi);
        }
        else if (rasterize)
        {
            try
            {
                (_, _, png) = Rasterize(source, page.Number - 1, dpi);
            }
            catch
            {
                png = null;
            }
        }

        if (png is not null)
        {
            using var decoded = SkiaSharp.SKBitmap.Decode(png);
            if (decoded is not null)
            {
                pixelW = decoded.Width;
                pixelH = decoded.Height;
                words = ExtractWords(page, dpi, pixelW, pixelH);
            }
        }

        return new IngestedPage
        {
            PageIndex = page.Number - 1,
            PixelWidth = pixelW,
            PixelHeight = pixelH,
            Dpi = dpi,
            Kind = useText ? PageSourceKind.EmbeddedText : PageSourceKind.Ocr,
            RasterPng = png,
            EmbeddedWords = useText ? words : []
        };
    }

    private static List<EmbeddedWord> ExtractWords(Page page, int dpi, int pixelW, int pixelH)
    {
        var result = new List<EmbeddedWord>();
        foreach (var word in page.GetWords())
        {
            var text = word.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var box = word.BoundingBox;
            var pixel = CoordinateMapper.PdfToPixel(box.Left, box.Bottom, box.Width, box.Height, page.Height, dpi);
            pixel = ClampToPage(pixel, pixelW, pixelH);
            result.Add(new EmbeddedWord { Text = text, BoundingBox = pixel });
        }

        return result;
    }

    private static PixelBox ClampToPage(PixelBox box, int width, int height)
    {
        var x = Math.Clamp(box.X, 0, width);
        var y = Math.Clamp(box.Y, 0, height);
        var r = Math.Clamp(box.Right, 0, width);
        var b = Math.Clamp(box.Bottom, 0, height);
        return new PixelBox(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
    }

    private static (int Width, int Height, byte[] Png) Rasterize(DocumentSource source, int pageIndex, int dpi)
    {
        var options = new RenderOptions { Dpi = dpi };
        using var stream = source.OpenRead();
        using var bitmap = Conversion.ToImage(stream, page: pageIndex, leaveOpen: false, password: null, options: options);
        return RasterCodec.Encode(bitmap);
    }
}
