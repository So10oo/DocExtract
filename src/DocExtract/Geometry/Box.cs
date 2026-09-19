namespace DocExtract;

/// <summary>
/// Нормализованный прямоугольник в координатах 0..1 относительно страницы (лево-верх — начало).
/// Не зависит от DPI и удобен для шаблонов зон.
/// </summary>
public readonly record struct Box(double X, double Y, double Width, double Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public static Box Normalized(double x, double y, double width, double height) =>
        new(Clamp01(x), Clamp01(y), Math.Max(0, width), Math.Max(0, height));

    public Box Intersect(Box other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var r = Math.Min(Right, other.Right);
        var b = Math.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? default : new Box(x, y, r - x, b - y);
    }

    public bool Intersects(Box other) => !Intersect(other).IsEmpty;

    public Box Clamp() =>
        Normalized(X, Y, Width, Height);

    private static double Clamp01(double v) => Math.Clamp(v, 0, 1);
}
