using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using QuestPdfDocument = QuestPDF.Fluent.Document;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace DocExtract.Tests;

public class IngestTests
{
    public IngestTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Pdf_EmbeddedText_IsExtractedWithoutOcr()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dex-{Guid.NewGuid():N}.pdf");
        QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.Content().Text("Hello DocExtract");
            });
        }).GeneratePdf(path);
        try
        {
            using var extractor = new DocumentExtractor(new ExtractorOptions { PreferEmbeddedText = true, RenderDpi = 72 });
            using var source = DocumentSource.FromFile(path);
            var result = await extractor.ExtractAsync(source);
            Assert.Equal(1, result.SchemaVersion);
            Assert.Single(result.Pages);
            Assert.Equal(PageSourceKind.EmbeddedText, result.Pages[0].SourceKind);
            Assert.Contains("Hello", result.ToPlainText(), StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(result.Pages[0].Blocks);
            Assert.True(result.Pages[0].Blocks[0].Lines[0].Words[0].NormalizedBox.Width > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Docx_Paragraphs_BecomeEmbeddedText()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dex-{Guid.NewGuid():N}.docx");
        CreateDocx(path, "DocExtract тест RU and EN");
        try
        {
            using var extractor = new DocumentExtractor(new ExtractorOptions { PreferEmbeddedText = true });
            using var source = DocumentSource.FromFile(path);
            var result = await extractor.ExtractAsync(source);
            Assert.Contains("DocExtract", result.ToPlainText());
            Assert.Equal(PageSourceKind.EmbeddedText, result.Pages[0].SourceKind);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Png_UsesFakeOcrEngine()
    {
        var png = SolidPng(400, 200);
        using var engine = new FakeOcrEngine { Text = "OCR-OK" };
        using var extractor = new DocumentExtractor(engine, new ExtractorOptions { PreferEmbeddedText = false });
        using var source = DocumentSource.FromBytes(png, "page.png");
        var result = await extractor.ExtractAsync(source);
        Assert.Equal(PageSourceKind.Ocr, result.Pages[0].SourceKind);
        Assert.Contains("OCR-OK", result.ToPlainText());
    }

    [Fact]
    public async Task ScanPdf_GoesThroughOcrEngine()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dex-scan-{Guid.NewGuid():N}.pdf");
        CreateImageOnlyPdf(path);
        try
        {
            using var engine = new FakeOcrEngine { Text = "SCAN" };
            using var extractor = new DocumentExtractor(engine, new ExtractorOptions
            {
                PreferEmbeddedText = true,
                EmbeddedTextThreshold = 30,
                RenderDpi = 72
            });
            using var source = DocumentSource.FromFile(path);
            var result = await extractor.ExtractAsync(source);
            Assert.Contains("SCAN", result.ToPlainText());
            Assert.Equal(PageSourceKind.Ocr, result.Pages[0].SourceKind);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExtractRegion_OffsetsCoordinatesToFullPage()
    {
        var png = SolidPng(200, 100);
        using var engine = new FakeOcrEngine { Text = "SEG" };
        using var extractor = new DocumentExtractor(engine);
        using var source = DocumentSource.FromBytes(png, "seg.png");
        var region = new PageRegion(0, Box.Normalized(0.1, 0.1, 0.4, 0.4));
        var result = await extractor.ExtractRegionAsync(source, region);
        Assert.Contains("SEG", result.ToPlainText());
        Assert.Equal(200, result.Page.PixelWidth);
        var word = result.Page.Blocks.SelectMany(b => b.Lines).SelectMany(l => l.Words).First();
        Assert.True(word.BoundingBox.X >= 19);
        Assert.Equal(1, result.SchemaVersion);
        Assert.Contains("schemaVersion", result.ToJson());
    }

    [Fact]
    public async Task ExtractFields_IsReserved()
    {
        using var extractor = new DocumentExtractor();
        using var source = DocumentSource.FromBytes(SolidPng(10, 10), "a.png");
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            extractor.ExtractFieldsAsync(source, new FieldSchema()));
    }

    private static void CreateDocx(string path, string text)
    {
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(text)))));
        main.Document.Save();
    }

    private static byte[] SolidPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 24);
        canvas.DrawText("Aa", 20, 40, SKTextAlign.Left, font, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static void CreateImageOnlyPdf(string path)
    {
        var png = SolidPng(300, 400);
        QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(300, 400);
                page.Content().Image(png);
            });
        }).GeneratePdf(path);
    }
}
