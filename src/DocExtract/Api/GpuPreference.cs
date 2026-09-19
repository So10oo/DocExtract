using System.Text.Json.Serialization;

namespace DocExtract;

/// <summary>
/// Предпочтение GPU для локального OCR-движка.
/// </summary>
[JsonConverter(typeof(CamelEnumConverter<GpuPreference>))]
public enum GpuPreference
{
    /// <summary>Только CPU.</summary>
    None = 0,

    /// <summary>Попробовать DirectML, при неудаче — CPU.</summary>
    Auto = 1,

    /// <summary>Принудительно DirectML (Windows).</summary>
    DirectML = 2
}
