namespace DocExtract.Tests;

public class GeometryTests
{
    [Fact]
    public void Normalized_And_Pixel_RoundTrip()
    {
        var box = Box.Normalized(0.1, 0.2, 0.3, 0.4);
        var pixel = CoordinateMapper.ToPixel(box, 1000, 2000);
        var back = CoordinateMapper.ToNormalized(pixel, 1000, 2000);

        Assert.Equal(box.X, back.X, 5);
        Assert.Equal(box.Y, back.Y, 5);
        Assert.Equal(box.Width, back.Width, 5);
        Assert.Equal(box.Height, back.Height, 5);
    }

    [Fact]
    public void PdfToPixel_FlipsOrigin()
    {
        var pixel = CoordinateMapper.PdfToPixel(72, 72, 72, 36, pageHeightPoints: 792, dpi: 72);
        Assert.Equal(72, pixel.X, 1);
        Assert.Equal(792 - 72 - 36, pixel.Y, 1);
        Assert.Equal(72, pixel.Width, 1);
        Assert.Equal(36, pixel.Height, 1);
    }

    [Fact]
    public void Box_Intersect()
    {
        var a = new Box(0.1, 0.1, 0.5, 0.5);
        var b = new Box(0.4, 0.4, 0.5, 0.5);
        var i = a.Intersect(b);
        Assert.False(i.IsEmpty);
        Assert.Equal(0.4, i.X, 5);
        Assert.Equal(0.4, i.Y, 5);
    }
}
