using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocExtract.Desktop.Services;

public sealed class AppSettings
{
    public string UiLanguage { get; set; } = "ru";
    public bool LanguageRu { get; set; } = true;
    public bool LanguageEn { get; set; } = true;
    public GpuPreference Gpu { get; set; } = GpuPreference.Auto;
    public int RenderDpi { get; set; } = 200;
    public bool PreferEmbeddedText { get; set; } = true;
    public string? ExportFolder { get; set; }
    public string? ModelsDirectory { get; set; }

    public static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocExtract", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch
        {
            // настройки по умолчанию
        }

        return new AppSettings();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SettingsJsonContext.Default.AppSettings));
    }

    public ExtractorOptions ToExtractorOptions(DocExtract.Ocr.IOcrEngine? engine) => new()
    {
        Languages = BuildLanguages(),
        Gpu = Gpu,
        RenderDpi = Math.Clamp(RenderDpi, 72, 400),
        PreferEmbeddedText = PreferEmbeddedText,
        ModelsDirectory = ModelsDirectory,
        Engine = engine
    };

    public IReadOnlyList<string> BuildLanguages()
    {
        var list = new List<string>();
        if (LanguageRu)
        {
            list.Add("ru");
        }

        if (LanguageEn)
        {
            list.Add("en");
        }

        return list.Count == 0 ? ["ru", "en"] : list;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
