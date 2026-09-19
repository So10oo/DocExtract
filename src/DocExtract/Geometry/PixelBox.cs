namespace DocExtract;

/// <summary>
/// Прямоугольник в пикселях растра страницы (начало — левый верхний угол).
/// </summary>
public readonly record struct PixelBox(double X, double Y, double Width, double Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public PixelBox Offset(double dx, double dy) => new(X + dx, Y + dy, Width, Height);

    public PixelBox Intersect(PixelBox other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var r = Math.Min(Right, other.Right);
        var b = Math.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? default : new PixelBox(x, y, r - x, b - y);
    }
}
