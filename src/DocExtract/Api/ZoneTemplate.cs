using System.Text.Json;

namespace DocExtract;

/// <summary>
/// Визуальный шаблон зон (.dex-template.json). В MVP сохраняется и загружается;
/// извлечение значений полей по шаблону — этап 1.
/// </summary>
public sealed class ZoneTemplate
{
    public int SchemaVersion { get; init; } = 1;

    public string Name { get; init; } = "template";

    public IReadOnlyList<TemplateZone> Zones { get; init; } = [];

    public static ZoneTemplate FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize(json, DocExtractJsonContext.Default.ZoneTemplate)
               ?? throw new InvalidOperationException("Не удалось разобрать шаблон зон.");
    }

    public static ZoneTemplate Load(string path) => FromJson(File.ReadAllText(path));

    public string ToJson() => JsonSerializer.Serialize(this, DocExtractJsonContext.Default.ZoneTemplate);

    public void Save(string path) => File.WriteAllText(path, ToJson());
}

public sealed class TemplateZone
{
    public required string Name { get; init; }

    public int PageIndex { get; init; }

    public Box NormalizedBounds { get; init; }

    public FieldValueType? FieldType { get; init; }
}
