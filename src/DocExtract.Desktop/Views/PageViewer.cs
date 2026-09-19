using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DocExtract.Desktop.ViewModels;

namespace DocExtract.Desktop.Views;

public sealed class PageViewer : Control
{
    public static readonly StyledProperty<Bitmap?> PageImageProperty =
        AvaloniaProperty.Register<PageViewer, Bitmap?>(nameof(PageImage));

    public static readonly StyledProperty<IReadOnlyList<OverlayRect>?> OverlaysProperty =
        AvaloniaProperty.Register<PageViewer, IReadOnlyList<OverlayRect>?>(nameof(Overlays));

    public static readonly StyledProperty<bool> SelectionEnabledProperty =
        AvaloniaProperty.Register<PageViewer, bool>(nameof(SelectionEnabled));

    public event EventHandler<Box>? SelectionCompleted;

    private Point? _start;
    private Point? _current;

    static PageViewer()
    {
        AffectsRender<PageViewer>(PageImageProperty, OverlaysProperty);
    }

    public Bitmap? PageImage
    {
        get => GetValue(PageImageProperty);
        set => SetValue(PageImageProperty, value);
    }

    public IReadOnlyList<OverlayRect>? Overlays
    {
        get => GetValue(OverlaysProperty);
        set => SetValue(OverlaysProperty, value);
    }

    public bool SelectionEnabled
    {
        get => GetValue(SelectionEnabledProperty);
        set => SetValue(SelectionEnabledProperty, value);
    }

    public PageViewer()
    {
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var dest = ImageDest();
        if (PageImage is not null && dest.Width > 0 && dest.Height > 0)
        {
            context.DrawImage(PageImage, dest);
        }

        if (Overlays is not null)
        {
            foreach (var overlay in Overlays)
            {
                var rect = ToView(overlay.Bounds, dest);
                var color = overlay.Kind switch
                {
                    OverlayKind.Zone => Color.FromArgb(60, 30, 144, 255),
                    OverlayKind.Selection => Color.FromArgb(80, 255, 165, 0),
                    _ => Color.FromArgb(50, 50, 205, 50)
                };
                var stroke = overlay.Kind switch
                {
                    OverlayKind.Zone => Colors.DodgerBlue,
                    OverlayKind.Selection => Colors.Orange,
                    _ => Colors.LimeGreen
                };
                context.FillRectangle(new SolidColorBrush(color), rect);
                context.DrawRectangle(new Pen(new SolidColorBrush(stroke), 1.5), rect);
                if (!string.IsNullOrWhiteSpace(overlay.Label))
                {
                    var text = new FormattedText(
                        overlay.Label,
                        System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Inter"),
                        12,
                        Brushes.White);
                    context.DrawText(text, rect.TopLeft);
                }
            }
        }

        if (_start is { } s && _current is { } c)
        {
            var rect = new Rect(s, c);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(70, 255, 140, 0)), rect);
            context.DrawRectangle(new Pen(Brushes.Orange, 2), rect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!SelectionEnabled)
        {
            return;
        }

        _start = e.GetPosition(this);
        _current = _start;
        e.Pointer.Capture(this);
        InvalidateVisual();
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_start is null)
        {
            return;
        }

        _current = e.GetPosition(this);
        InvalidateVisual();
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_start is { } s && _current is { } c)
        {
            var dest = ImageDest();
            var view = new Rect(s, c);
            if (dest.Width > 1 && dest.Height > 1 && view.Width > 2 && view.Height > 2)
            {
                var nx = (view.X - dest.X) / dest.Width;
                var ny = (view.Y - dest.Y) / dest.Height;
                var nw = view.Width / dest.Width;
                var nh = view.Height / dest.Height;
                var box = Box.Normalized(nx, ny, nw, nh);
                if (!box.IsEmpty)
                {
                    SelectionCompleted?.Invoke(this, box);
                }
            }
        }

        _start = null;
        _current = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
        base.OnPointerReleased(e);
    }

    private Rect ImageDest()
    {
        if (PageImage is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return default;
        }

        var iw = PageImage.PixelSize.Width;
        var ih = PageImage.PixelSize.Height;
        var scale = Math.Min(Bounds.Width / iw, Bounds.Height / ih);
        var w = iw * scale;
        var h = ih * scale;
        var x = (Bounds.Width - w) / 2;
        var y = (Bounds.Height - h) / 2;
        return new Rect(x, y, w, h);
    }

    private Rect ToView(Rect pagePixels, Rect dest)
    {
        if (PageImage is null || dest.Width <= 0 || dest.Height <= 0)
        {
            return default;
        }

        var sx = dest.Width / PageImage.PixelSize.Width;
        var sy = dest.Height / PageImage.PixelSize.Height;
        return new Rect(
            dest.X + pagePixels.X * sx,
            dest.Y + pagePixels.Y * sy,
            pagePixels.Width * sx,
            pagePixels.Height * sy);
    }
}
