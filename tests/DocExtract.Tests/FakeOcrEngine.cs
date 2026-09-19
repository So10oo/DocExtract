using DocExtract.Ocr;

namespace DocExtract.Tests;

internal sealed class FakeOcrEngine : IOcrEngine
{
    public string Name => "fake";

    public string Text { get; init; } = "FAKE";

    public Task<PageOcrResult> RecognizeAsync(OcrImage image, OcrRequest request, CancellationToken cancellationToken = default)
    {
        var box = new PixelBox(0, 0, image.Width, Math.Max(12, image.Height / 8.0));
        return Task.FromResult(new PageOcrResult
        {
            Text = Text,
            Lines =
            [
                new OcrLine
                {
                    Text = Text,
                    Confidence = 0.99f,
                    BoundingBox = box,
                    Words = [new OcrWord { Text = Text, Confidence = 0.99f, BoundingBox = box }]
                }
            ]
        });
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
