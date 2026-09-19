namespace DocExtract.Pipeline;

internal readonly record struct LayoutWord(string Text, PixelBox Box, float Confidence);

internal static class TextLayoutBuilder
{
    public static IReadOnlyList<TextBlock> FromWords(IReadOnlyList<LayoutWord> words, int pageWidth, int pageHeight)
    {
        if (words.Count == 0)
        {
            return [];
        }

        var ordered = words
            .OrderBy(w => w.Box.Y)
            .ThenBy(w => w.Box.X)
            .ToList();

        var lines = new List<List<LayoutWord>>();
        foreach (var word in ordered)
        {
            if (lines.Count == 0)
            {
                lines.Add([word]);
                continue;
            }

            var current = lines[^1];
            var sample = current[0];
            var threshold = Math.Max(8, Math.Max(sample.Box.Height, word.Box.Height) * 0.6);
            if (Math.Abs(word.Box.Y - sample.Box.Y) <= threshold)
            {
                current.Add(word);
            }
            else
            {
                lines.Add([word]);
            }
        }

        var textLines = lines.Select(line => ToLine(line.OrderBy(w => w.Box.X).ToList(), pageWidth, pageHeight)).ToList();
        var blockBox = Union(textLines.Select(l => l.BoundingBox));
        var text = string.Join(Environment.NewLine, textLines.Select(l => l.Text));
        var confidence = textLines.Count == 0 ? 0 : textLines.Average(l => l.Confidence);

        return
        [
            new TextBlock
            {
                Text = text,
                Confidence = (float)confidence,
                BoundingBox = blockBox,
                NormalizedBox = CoordinateMapper.ToNormalized(blockBox, pageWidth, pageHeight),
                Lines = textLines
            }
        ];
    }

    public static IReadOnlyList<TextBlock> FromOcrLines(IReadOnlyList<Ocr.OcrLine> lines, int pageWidth, int pageHeight, double offsetX = 0, double offsetY = 0)
    {
        var words = new List<LayoutWord>();
        foreach (var line in lines)
        {
            if (line.Words.Count > 0)
            {
                foreach (var word in line.Words)
                {
                    words.Add(new LayoutWord(word.Text, word.BoundingBox.Offset(offsetX, offsetY), word.Confidence));
                }
            }
            else if (!string.IsNullOrWhiteSpace(line.Text))
            {
                words.AddRange(SplitLineToWords(line, offsetX, offsetY));
            }
        }

        return FromWords(words, pageWidth, pageHeight);
    }

    private static IEnumerable<LayoutWord> SplitLineToWords(Ocr.OcrLine line, double offsetX, double offsetY)
    {
        var parts = line.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            yield break;
        }

        var box = line.BoundingBox.Offset(offsetX, offsetY);
        var total = Math.Max(1, parts.Sum(p => p.Length) + (parts.Length - 1));
        var cursor = 0.0;
        foreach (var part in parts)
        {
            var share = (part.Length + 1.0) / total;
            var width = box.Width * share;
            yield return new LayoutWord(part, new PixelBox(box.X + cursor, box.Y, width, box.Height), line.Confidence);
            cursor += width;
        }
    }

    private static TextLine ToLine(IReadOnlyList<LayoutWord> words, int pageWidth, int pageHeight)
    {
        var box = Union(words.Select(w => w.Box));
        var text = string.Join(' ', words.Select(w => w.Text));
        var confidence = (float)words.Average(w => w.Confidence);
        return new TextLine
        {
            Text = text,
            Confidence = confidence,
            BoundingBox = box,
            NormalizedBox = CoordinateMapper.ToNormalized(box, pageWidth, pageHeight),
            Words = words.Select(w => new TextWord
            {
                Text = w.Text,
                Confidence = w.Confidence,
                BoundingBox = w.Box,
                NormalizedBox = CoordinateMapper.ToNormalized(w.Box, pageWidth, pageHeight)
            }).ToArray()
        };
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
}
