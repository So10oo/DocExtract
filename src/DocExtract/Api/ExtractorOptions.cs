using DocExtract.Ocr;
using Microsoft.Extensions.Logging;

namespace DocExtract;

/// <summary>
/// Настройки извлечения текста.
/// </summary>
public sealed class ExtractorOptions
{
    /// <summary>
    /// Языки распознавания. В MVP локальный движок использует смешанную модель RU+EN (ESLAV).
    /// </summary>
    public IReadOnlyList<string> Languages { get; init; } = ["ru", "en"];

    /// <summary>
    /// Если true, текстовый слой PDF используется вместо OCR на страницах с достаточным количеством символов.
    /// </summary>
    public bool PreferEmbeddedText { get; init; } = true;

    /// <summary>
    /// Минимальная длина извлечённого текстового слоя PDF, чтобы считать страницу «цифровой».
    /// </summary>
    public int EmbeddedTextThreshold { get; init; } = 8;

    /// <summary>GPU: CPU / Auto (DirectML) / DirectML.</summary>
    public GpuPreference Gpu { get; init; } = GpuPreference.Auto;

    /// <summary>DPI растра страницы PDF и превью.</summary>
    public int RenderDpi { get; init; } = 200;

    /// <summary>Параллелизм OCR-страниц в одном документе.</summary>
    public int MaxDegreeOfParallelism { get; init; } = Math.Min(4, Environment.ProcessorCount);

    /// <summary>
    /// Каталог с ONNX-моделями. Если null — ищутся в <c>models/</c> рядом с приложением
    /// и в переменной окружения <c>DOCEXTRACT_MODELS</c>.
    /// </summary>
    public string? ModelsDirectory { get; init; }

    /// <summary>Необязательный свой движок. Если null, OCR потребуется только для сканов.</summary>
    public IOcrEngine? Engine { get; init; }

    /// <summary>Логгер SDK (опционально).</summary>
    public ILogger? Logger { get; init; }
}
