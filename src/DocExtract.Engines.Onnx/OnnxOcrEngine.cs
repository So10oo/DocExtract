using DocExtract.Ocr;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using RapidOCRSharpOnnx;
using RapidOCRSharpOnnx.Configurations;
using RapidOCRSharpOnnx.Inference.PPOCR_Det.Models;
using RapidOCRSharpOnnx.Providers;
using RapidOCRSharpOnnx.Utils;

namespace DocExtract.Engines.Onnx;

/// <summary>
/// Локальный PP-OCR (ONNX Runtime): CPU по умолчанию, DirectML при GpuPreference.Auto/DirectML.
/// Модель распознавания ESLAV покрывает русский и латиницу/цифры.
/// </summary>
public sealed class OnnxOcrEngine : IOcrEngine
{
    private readonly RapidOCRSharp _ocr;
    private readonly Lock _gate = new();
    private bool _disposed;

    public OnnxOcrEngine(RapidOCRSharp ocr)
    {
        _ocr = ocr;
    }

    public string Name => "onnx-ppocr-eslav";

    public static OnnxOcrEngine Create(ExtractorOptions? options = null)
    {
        options ??= new ExtractorOptions();
        var dir = ModelLocator.ResolveDirectory(options.ModelsDirectory);
        var config = new OcrConfig(
            ModelLocator.DetPath(dir),
            ModelLocator.RecPath(dir),
            LangRec.ESLAV,
            OCRVersion.PPOCRV5,
            ModelLocator.ClsPath(dir))
        {
            ReturnWordBox = true
        };

        RapidOCRSharp ocr;
        if (options.Gpu == GpuPreference.None)
        {
            ocr = new RapidOCRSharp(new ExecutionProviderCPU(config));
        }
        else
        {
            try
            {
                ocr = new RapidOCRSharp(new ExecutionProviderDirectML(config, deviceId: 0));
            }
            catch (Exception ex) when (options.Gpu == GpuPreference.Auto)
            {
                options.Logger?.LogFallback(ex);
                ocr = new RapidOCRSharp(new ExecutionProviderCPU(config));
            }
        }

        return new OnnxOcrEngine(ocr);
    }

    public Task<PageOcrResult> RecognizeAsync(OcrImage image, OcrRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(image);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var mat = Cv2.ImDecode(image.PngBytes, ImreadModes.Color);
            if (mat.Empty())
            {
                throw new InvalidOperationException("OpenCV не смог декодировать растр страницы.");
            }

            RapidOCRSharpOnnx.OcrResult result;
            lock (_gate)
            {
                result = _ocr.RecognizeText(mat);
            }

            return Map(result, image.Width, image.Height);
        }, cancellationToken);
    }

    private static PageOcrResult Map(RapidOCRSharpOnnx.OcrResult result, int width, int height)
    {
        var lines = new List<OcrLine>();
        var wordItems = result.WordResults ?? [];
        if (wordItems.Length > 0)
        {
            foreach (var group in wordItems.GroupBy(w => w.LineId).OrderBy(g => g.Min(BoxTop)))
            {
                var words = group
                    .OrderBy(BoxLeft)
                    .Select(item => new OcrWord
                    {
                        Text = item.Word ?? "",
                        Confidence = item.Score,
                        BoundingBox = ToPixelBox(item.Box, width, height)
                    })
                    .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                    .ToArray();
                if (words.Length == 0)
                {
                    continue;
                }

                var box = Union(words.Select(w => w.BoundingBox));
                lines.Add(new OcrLine
                {
                    Text = string.Join(' ', words.Select(w => w.Text)),
                    Confidence = words.Average(w => w.Confidence),
                    BoundingBox = box,
                    Words = words
                });
            }
        }
        else if (result.RecResult?.Data is { Length: > 0 } recs)
        {
            foreach (var rec in recs)
            {
                if (string.IsNullOrWhiteSpace(rec.Label))
                {
                    continue;
                }

                lines.Add(new OcrLine
                {
                    Text = rec.Label,
                    Confidence = rec.Score,
                    BoundingBox = new PixelBox(0, 0, width, Math.Max(1, height / Math.Max(1, recs.Length))),
                    Words = []
                });
            }
        }

        var text = !string.IsNullOrWhiteSpace(result.TextBlocks)
            ? result.TextBlocks.Replace("\r\n", "\n").Trim()
            : string.Join('\n', lines.Select(l => l.Text));

        return new PageOcrResult { Text = text, Lines = lines };
    }

    private static double BoxLeft(DetBoxItem item) => item.Box is { Length: > 0 } ? item.Box.Min(p => p.X) : 0;

    private static double BoxTop(DetBoxItem item) => item.Box is { Length: > 0 } ? item.Box.Min(p => p.Y) : 0;

    private static PixelBox ToPixelBox(OpenCvSharp.Point2f[]? points, int width, int height)
    {
        if (points is null || points.Length == 0)
        {
            return default;
        }

        var x = points.Min(p => p.X);
        var y = points.Min(p => p.Y);
        var r = points.Max(p => p.X);
        var b = points.Max(p => p.Y);
        x = Math.Clamp(x, 0, width);
        y = Math.Clamp(y, 0, height);
        r = Math.Clamp(r, 0, width);
        b = Math.Clamp(b, 0, height);
        return new PixelBox(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
    }

    private static PixelBox Union(IEnumerable<PixelBox> boxes)
    {
        var list = boxes.Where(b => !b.IsEmpty).ToList();
        if (list.Count == 0)
        {
            return default;
        }

        var x = list.Min(b => b.X);
        var y = list.Min(b => b.Y);
        var r = list.Max(b => b.Right);
        var btm = list.Max(b => b.Bottom);
        return new PixelBox(x, y, r - x, btm - y);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ocr.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

file static class OnnxLog
{
    public static void LogFallback(this Microsoft.Extensions.Logging.ILogger? logger, Exception ex) =>
        logger?.LogWarning(ex, "DirectML недоступен, OCR переключается на CPU.");
}
