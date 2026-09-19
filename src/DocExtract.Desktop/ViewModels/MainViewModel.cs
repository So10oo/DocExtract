using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocExtract.Desktop.Localization;
using DocExtract.Desktop.Services;
using DocExtract.Engines.Onnx;
using DocExtract.Ocr;

namespace DocExtract.Desktop.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private DocumentExtractor? _extractor;
    private IOcrEngine? _engine;
    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        Settings = AppSettings.Load();
        Loc.Language = Settings.UiLanguage;
        GpuChoice = Settings.Gpu == GpuPreference.None ? "Off" : "Auto";
        RecreateExtractor();
    }

    public I18n Loc => I18n.Current;

    public Window? Host { get; set; }

    public AppSettings Settings { get; }

    public ObservableCollection<QueueItemViewModel> Queue { get; } = [];

    public ObservableCollection<ZoneItemViewModel> Zones { get; } = [];

    public ObservableCollection<OverlayRect> Overlays { get; } = [];

    [ObservableProperty] public partial IReadOnlyList<OverlayRect> OverlaySnapshot { get; set; } = [];

    [ObservableProperty] public partial QueueItemViewModel? SelectedItem { get; set; }
    [ObservableProperty] public partial Bitmap? PageBitmap { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; } = I18n.Current.Ready;
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial bool ShowOverlay { get; set; } = true;
    [ObservableProperty] public partial bool SelectionEnabled { get; set; }
    [ObservableProperty] public partial bool DrawZoneMode { get; set; }
    [ObservableProperty] public partial string NewZoneName { get; set; } = "field1";
    [ObservableProperty] public partial string GpuChoice { get; set; } = "Auto";

    partial void OnSelectedItemChanged(QueueItemViewModel? value) => _ = LoadPreviewAsync();

    partial void OnShowOverlayChanged(bool value) => RefreshOverlays();

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        if (Host is null)
        {
            return;
        }

        var files = await Host.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc.AddFiles,
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Documents")
                {
                    Patterns = ["*.pdf", "*.docx", "*.xlsx", "*.png", "*.jpg", "*.jpeg", "*.tif", "*.tiff", "*.webp", "*.bmp"]
                }
            ]
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(path) && Queue.All(q => q.Path != path))
            {
                Queue.Add(new QueueItemViewModel(path));
            }
        }
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        if (Host is null)
        {
            return;
        }

        var folders = await Host.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Loc.AddFolder,
            AllowMultiple = false
        });
        var folder = folders.FirstOrDefault();
        if (folder is null)
        {
            return;
        }

        var path = folder.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => DocumentIngestExtensions.IsSupported(f));
        foreach (var file in files)
        {
            if (Queue.All(q => q.Path != file))
            {
                Queue.Add(new QueueItemViewModel(file));
            }
        }
    }

    [RelayCommand]
    private async Task ProcessQueueAsync()
    {
        if (_extractor is null)
        {
            return;
        }

        IsBusy = true;
        StatusText = Loc.Processing;
        _cts = new CancellationTokenSource();
        try
        {
            foreach (var item in Queue.Where(i => i.Status is QueueStatus.Pending or QueueStatus.Error).ToList())
            {
                await ProcessItemAsync(item, _cts.Token).ConfigureAwait(true);
            }

            StatusText = Loc.Ready;
        }
        catch (OperationCanceledException)
        {
            StatusText = Loc.Ready;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RetrySelectedAsync()
    {
        if (SelectedItem is null || _extractor is null)
        {
            return;
        }

        SelectedItem.Status = QueueStatus.Pending;
        await ProcessItemAsync(SelectedItem, CancellationToken.None);
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        Queue.Remove(SelectedItem);
        SelectedItem = Queue.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ExportSelectedAsync()
    {
        if (SelectedItem?.Result is null)
        {
            return;
        }

        var folder = Settings.ExportFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            if (Host is null)
            {
                return;
            }

            var dirs = await Host.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
            folder = dirs.FirstOrDefault()?.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            Settings.ExportFolder = folder;
        }

        Directory.CreateDirectory(folder);
        var name = Path.GetFileNameWithoutExtension(SelectedItem.FileName);
        var txt = Path.Combine(folder, name + ".txt");
        var json = Path.Combine(folder, name + ".json");
        await File.WriteAllTextAsync(txt, SelectedItem.EditedText);
        var corrected = SelectedItem.EditedText == SelectedItem.Result.ToPlainText() ? null : SelectedItem.EditedText;
        await File.WriteAllTextAsync(json, SelectedItem.Result.ToJson(corrected));
        StatusText = folder;
    }

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (SelectedItem is { CurrentPage: > 0 })
        {
            SelectedItem.CurrentPage--;
            await LoadPreviewAsync();
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (SelectedItem is { } item && item.CurrentPage + 1 < Math.Max(item.PageCount, 1))
        {
            item.CurrentPage++;
            await LoadPreviewAsync();
        }
    }

    [RelayCommand]
    private void ToggleSelection()
    {
        SelectionEnabled = !SelectionEnabled;
        if (SelectionEnabled)
        {
            DrawZoneMode = false;
        }
    }

    [RelayCommand]
    private void ToggleDrawZone()
    {
        DrawZoneMode = !DrawZoneMode;
        SelectionEnabled = DrawZoneMode || SelectionEnabled;
        if (DrawZoneMode)
        {
            SelectionEnabled = true;
        }
    }

    public async Task OnSelectionCompletedAsync(Box normalized)
    {
        if (SelectedItem is null || _extractor is null)
        {
            return;
        }

        if (DrawZoneMode)
        {
            Zones.Add(new ZoneItemViewModel
            {
                Name = string.IsNullOrWhiteSpace(NewZoneName) ? $"zone{Zones.Count + 1}" : NewZoneName,
                PageIndex = SelectedItem.CurrentPage,
                NormalizedBounds = normalized
            });
            RefreshOverlays();
            return;
        }

        try
        {
            IsBusy = true;
            using var source = DocumentSource.FromFile(SelectedItem.Path);
            var region = new PageRegion(SelectedItem.CurrentPage, normalized);
            var result = await _extractor.ExtractRegionAsync(source, region);
            SelectedItem.EditedText = result.ToPlainText();
            StatusText = result.ToPlainText();
            RefreshOverlays(result.Page);
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (Host is null)
        {
            return;
        }

        var file = await Host.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Loc.SaveTemplate,
            DefaultExtension = "dex-template.json",
            FileTypeChoices = [new FilePickerFileType("DocExtract template") { Patterns = ["*.dex-template.json"] }]
        });
        if (file is null)
        {
            return;
        }

        var template = new ZoneTemplate
        {
            Name = Path.GetFileNameWithoutExtension(file.Name),
            Zones = Zones.Select(z => new TemplateZone
            {
                Name = z.Name,
                PageIndex = z.PageIndex,
                NormalizedBounds = z.NormalizedBounds,
                FieldType = z.FieldType
            }).ToArray()
        };
        await File.WriteAllTextAsync(file.Path.LocalPath, template.ToJson());
    }

    [RelayCommand]
    private async Task LoadTemplateAsync()
    {
        if (Host is null)
        {
            return;
        }

        var files = await Host.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Loc.LoadTemplate,
            FileTypeFilter = [new FilePickerFileType("DocExtract template") { Patterns = ["*.dex-template.json", "*.json"] }]
        });
        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var template = ZoneTemplate.Load(file.Path.LocalPath);
        Zones.Clear();
        foreach (var zone in template.Zones)
        {
            Zones.Add(new ZoneItemViewModel
            {
                Name = zone.Name,
                PageIndex = zone.PageIndex,
                NormalizedBounds = zone.NormalizedBounds,
                FieldType = zone.FieldType ?? FieldValueType.String
            });
        }

        RefreshOverlays();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        Settings.UiLanguage = Loc.Language;
        Settings.Gpu = GpuChoice == "Off" ? GpuPreference.None : GpuPreference.Auto;
        Settings.Save();
        RecreateExtractor();
        StatusText = Loc.Ready;
    }

    [RelayCommand]
    private void SetUiLanguage(string lang)
    {
        Loc.Language = lang;
        Settings.UiLanguage = lang;
    }

    private async Task ProcessItemAsync(QueueItemViewModel item, CancellationToken cancellationToken)
    {
        item.Status = QueueStatus.Running;
        item.ErrorMessage = null;
        try
        {
            using var source = DocumentSource.FromFile(item.Path);
            var result = await _extractor!.ExtractAsync(source, cancellationToken).ConfigureAwait(true);
            item.Result = result;
            item.PageCount = result.Pages.Count;
            item.CurrentPage = 0;
            item.EditedText = result.ToPlainText();
            item.Progress = 100;
            item.Status = QueueStatus.Done;
            AutoExport(item);
            if (ReferenceEquals(SelectedItem, item))
            {
                await LoadPreviewAsync();
            }
        }
        catch (Exception ex)
        {
            item.Status = QueueStatus.Error;
            item.ErrorMessage = ex.Message;
            StatusText = ex.Message;
        }
    }

    private void AutoExport(QueueItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(Settings.ExportFolder) || item.Result is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Settings.ExportFolder);
            var name = Path.GetFileNameWithoutExtension(item.FileName);
            File.WriteAllText(Path.Combine(Settings.ExportFolder, name + ".txt"), item.EditedText);
            File.WriteAllText(Path.Combine(Settings.ExportFolder, name + ".json"), item.Result.ToJson());
        }
        catch
        {
            // экспорт не должен ронять очередь
        }
    }

    private async Task LoadPreviewAsync()
    {
        PageBitmap?.Dispose();
        PageBitmap = null;
        Overlays.Clear();
        if (SelectedItem is null || _extractor is null)
        {
            return;
        }

        try
        {
            using var source = DocumentSource.FromFile(SelectedItem.Path);
            if (SelectedItem.PageCount == 0)
            {
                SelectedItem.PageCount = await _extractor.GetPageCountAsync(source);
            }

            var image = await _extractor.RenderPageAsync(source, SelectedItem.CurrentPage);
            await using var stream = new MemoryStream(image.PngBytes);
            PageBitmap = new Bitmap(stream);
            RefreshOverlays();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private void RefreshOverlays(PageResult? page = null)
    {
        Overlays.Clear();
        if (!ShowOverlay)
        {
            OverlaySnapshot = [];
            return;
        }

        page ??= SelectedItem?.Result?.Pages.ElementAtOrDefault(SelectedItem.CurrentPage);
        if (page is not null)
        {
            foreach (var word in page.Blocks.SelectMany(b => b.Lines).SelectMany(l => l.Words))
            {
                Overlays.Add(new OverlayRect(
                    new Rect(word.BoundingBox.X, word.BoundingBox.Y, word.BoundingBox.Width, word.BoundingBox.Height),
                    OverlayKind.Word,
                    word.Text));
            }
        }

        if (SelectedItem is not null)
        {
            foreach (var zone in Zones.Where(z => z.PageIndex == SelectedItem.CurrentPage))
            {
                var pixel = CoordinateMapper.ToPixel(zone.NormalizedBounds, page?.PixelWidth ?? PageBitmap?.PixelSize.Width ?? 1,
                    page?.PixelHeight ?? PageBitmap?.PixelSize.Height ?? 1);
                Overlays.Add(new OverlayRect(
                    new Rect(pixel.X, pixel.Y, pixel.Width, pixel.Height),
                    OverlayKind.Zone,
                    zone.Name));
            }
        }

        OverlaySnapshot = Overlays.ToArray();
    }

    private void RecreateExtractor()
    {
        _extractor?.Dispose();
        _engine?.Dispose();
        _engine = null;
        try
        {
            var options = Settings.ToExtractorOptions(null);
            if (GpuChoice == "Off")
            {
                options = new ExtractorOptions
                {
                    Languages = options.Languages,
                    Gpu = GpuPreference.None,
                    RenderDpi = options.RenderDpi,
                    PreferEmbeddedText = options.PreferEmbeddedText,
                    ModelsDirectory = options.ModelsDirectory
                };
            }

            if (ModelLocator.HasRequiredModels(ModelLocatorTry(options.ModelsDirectory)))
            {
                _engine = OnnxOcrEngine.Create(options);
                _extractor = new DocumentExtractor(_engine, options);
            }
            else
            {
                _extractor = new DocumentExtractor(options);
                StatusText = Loc.ModelsMissing;
            }
        }
        catch (Exception ex)
        {
            _extractor = new DocumentExtractor(Settings.ToExtractorOptions(null));
            StatusText = ex.Message;
        }
    }

    private static string ModelLocatorTry(string? explicitDir)
    {
        try
        {
            return ModelLocator.ResolveDirectory(explicitDir);
        }
        catch
        {
            return explicitDir ?? Path.Combine(AppContext.BaseDirectory, "models");
        }
    }
}

internal static class DocumentIngestExtensions
{
    public static bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".pdf" or ".docx" or ".xlsx" or ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".webp" or ".bmp";
    }
}
