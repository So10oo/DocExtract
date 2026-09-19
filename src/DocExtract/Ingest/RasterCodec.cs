using SkiaSharp;

namespace DocExtract.Ingest;

internal static class RasterCodec
{
    public static (int Width, int Height, byte[] Png) ToPng(byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes)
                           ?? throw new InvalidOperationException("Не удалось декодировать изображение (поддерживаются PNG, JPEG, WebP; TIFF — если кодек доступен).");
        return Encode(bitmap);
    }

    public static (int Width, int Height, byte[] Png) Encode(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return (bitmap.Width, bitmap.Height, data.ToArray());
    }

    public static byte[] CropPng(byte[] png, PixelBox crop)
    {
        using var bitmap = SKBitmap.Decode(png)
                           ?? throw new InvalidOperationException("Не удалось декодировать PNG для обрезки.");
        var x = (int)Math.Clamp(Math.Floor(crop.X), 0, Math.Max(0, bitmap.Width - 1));
        var y = (int)Math.Clamp(Math.Floor(crop.Y), 0, Math.Max(0, bitmap.Height - 1));
        var w = (int)Math.Clamp(Math.Ceiling(crop.Width), 1, bitmap.Width - x);
        var h = (int)Math.Clamp(Math.Ceiling(crop.Height), 1, bitmap.Height - y);
        var rect = new SKRectI(x, y, x + w, y + h);
        using var subset = new SKBitmap();
        if (!bitmap.ExtractSubset(subset, rect))
        {
            throw new InvalidOperationException("Не удалось вырезать сегмент страницы.");
        }

        var encoded = Encode(subset);
        return encoded.Png;
    }
}
