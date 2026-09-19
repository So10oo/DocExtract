namespace DocExtract;

/// <summary>
/// Перевод между пикселями растра, нормализованными 0..1 и PDF user space.
/// </summary>
public static class CoordinateMapper
{
    public const double PdfPointsPerInch = 72.0;

    public static Box ToNormalized(PixelBox pixel, int pageWidth, int pageHeight)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
        {
            return default;
        }

        return Box.Normalized(
            pixel.X / pageWidth,
            pixel.Y / pageHeight,
            pixel.Width / pageWidth,
            pixel.Height / pageHeight);
    }

    public static PixelBox ToPixel(Box normalized, int pageWidth, int pageHeight)
    {
        var box = normalized.Clamp();
        return new PixelBox(
            box.X * pageWidth,
            box.Y * pageHeight,
            box.Width * pageWidth,
            box.Height * pageHeight);
    }

    /// <summary>
    /// PDF: начало координат — левый нижний угол, единицы — пункты.
    /// Растр: левый верх, пиксели при заданном DPI.
    /// </summary>
    public static PixelBox PdfToPixel(
        double pdfX,
        double pdfY,
        double pdfWidth,
        double pdfHeight,
        double pageHeightPoints,
        int dpi)
    {
        var scale = dpi / PdfPointsPerInch;
        var x = pdfX * scale;
        var y = (pageHeightPoints - pdfY - pdfHeight) * scale;
        return new PixelBox(x, y, pdfWidth * scale, pdfHeight * scale);
    }

    public static (int Width, int Height) PagePixelSize(double pageWidthPoints, double pageHeightPoints, int dpi)
    {
        var scale = dpi / PdfPointsPerInch;
        return (
            Math.Max(1, (int)Math.Round(pageWidthPoints * scale)),
            Math.Max(1, (int)Math.Round(pageHeightPoints * scale)));
    }
}
