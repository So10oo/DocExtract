using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DocExtract.Desktop.ViewModels;

public enum OverlayKind
{
    Word,
    Zone,
    Selection
}

public sealed class OverlayRect
{
    public OverlayRect(Rect bounds, OverlayKind kind, string? label = null)
    {
        Bounds = bounds;
        Kind = kind;
        Label = label;
    }

    public Rect Bounds { get; }
    public OverlayKind Kind { get; }
    public string? Label { get; }
}

public enum QueueStatus
{
    Pending,
    Running,
    Done,
    Error
}

public sealed partial class QueueItemViewModel : ObservableObject
{
    public QueueItemViewModel(string path)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
    }

    public string Path { get; }
    public string FileName { get; }

    [ObservableProperty] public partial QueueStatus Status { get; set; } = QueueStatus.Pending;
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial int Progress { get; set; }
    [ObservableProperty] public partial int PageCount { get; set; }
    [ObservableProperty] public partial int CurrentPage { get; set; }
    [ObservableProperty] public partial DocumentResult? Result { get; set; }
    [ObservableProperty] public partial string EditedText { get; set; } = "";
}

public sealed partial class ZoneItemViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "zone";
    [ObservableProperty] public partial int PageIndex { get; set; }
    [ObservableProperty] public partial Box NormalizedBounds { get; set; }
    [ObservableProperty] public partial FieldValueType FieldType { get; set; } = FieldValueType.String;
}
