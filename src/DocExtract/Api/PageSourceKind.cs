using System.Text.Json.Serialization;

namespace DocExtract;

/// <summary>
/// Как получен текст страницы.
/// </summary>
[JsonConverter(typeof(CamelEnumConverter<PageSourceKind>))]
public enum PageSourceKind
{
    EmbeddedText,
    Ocr,
    Mixed,
    Empty
}
