using DocExtract.Engines.Onnx;

namespace DocExtract.Tests;

public class OcrIntegrationTests
{
    [Fact]
    public async Task OnnxEngine_RecognizesPrintedSample_WhenModelsPresent()
    {
        if (!TryFindModels(out var modelsDir))
        {
            return;
        }

        var options = new ExtractorOptions
        {
            ModelsDirectory = modelsDir,
            Gpu = GpuPreference.None,
            PreferEmbeddedText = false
        };
        using var engine = OnnxOcrEngine.Create(options);
        using var extractor = new DocumentExtractor(engine, options);
        var png = SamplePng();
        using var source = DocumentSource.FromBytes(png, "sample.png");
        var result = await extractor.ExtractAsync(source);
        Assert.False(string.IsNullOrWhiteSpace(result.ToPlainText()));
        Assert.NotEmpty(result.Pages[0].Blocks);
    }

    private static bool TryFindModels(out string directory)
    {
        try
        {
            directory = ModelLocator.ResolveDirectory();
            return true;
        }
        catch (FileNotFoundException)
        {
            directory = "";
            return false;
        }
    }

    private static byte[] SamplePng()
    {
        using var bitmap = new SkiaSharp.SKBitmap(400, 120);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.White);
        using var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true };
        using var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 32);
        canvas.DrawText("INVOICE 42", 24, 70, SkiaSharp.SKTextAlign.Left, font, paint);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
