using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocExtract;

/// <summary>
/// Тип значения структурированного поля (этап 1 roadmap).
/// </summary>
[JsonConverter(typeof(CamelEnumConverter<FieldValueType>))]
public enum FieldValueType
{
    String = 0,
    Number = 1,
    Date = 2
}

/// <summary>
/// Описание поля: имя, тип и опциональная зона.
/// </summary>
public sealed class FieldDefinition
{
    public required string Name { get; init; }

    public FieldValueType Type { get; init; } = FieldValueType.String;

    public PageRegion? Zone { get; init; }
}

/// <summary>
/// JSON-схема полей документа. Извлечение значений — этап 1.
/// </summary>
public sealed class FieldSchema
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

    public static FieldSchema FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize(json, DocExtractJsonContext.Default.FieldSchema)
               ?? throw new InvalidOperationException("Не удалось разобрать схему полей.");
    }

    public string ToJson() => JsonSerializer.Serialize(this, DocExtractJsonContext.Default.FieldSchema);
}

public sealed class ExtractedField
{
    public required string Name { get; init; }

    public string? Value { get; init; }

    public float Confidence { get; init; }

    public FieldValueType Type { get; init; }
}

public sealed class FieldExtractionResult
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<ExtractedField> Fields { get; init; } = [];
}
