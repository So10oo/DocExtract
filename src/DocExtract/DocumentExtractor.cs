using DocExtract.Ocr;
using DocExtract.Pipeline;

namespace DocExtract;

/// <summary>
/// Главная точка входа SDK: извлечение текста, сегменты, превью страниц.
/// Извлечение типизированных полей (<see cref="ExtractFieldsAsync"/>) будет реализовано на этапе 1.
/// </summary>
public sealed class DocumentExtractor : IAsyncDisposable, IDisposable
{
    private ExtractorOptions _options = null!;
    private ExtractionPipeline _pipeline = null!;
    private IOcrEngine? _ownedEngine;
    private bool _disposed;

    /// <summary>Создаёт экстрактор. Без движка доступен только текстовый слой PDF/Office.</summary>
    public DocumentExtractor(ExtractorOptions? options = null)
    {
        Init(options?.Engine, options, ownsEngine: false);
    }

    /// <summary>Создаёт экстрактор с явным OCR-движком.</summary>
    /// <param name="engine">Реализация <see cref="IOcrEngine"/>.</param>
    /// <param name="options">Опции пайплайна.</param>
    /// <param name="ownsEngine">Если true, движок будет освобождён вместе с экстрактором.</param>
    public DocumentExtractor(IOcrEngine engine, ExtractorOptions? options = null, bool ownsEngine = false)
    {
        ArgumentNullException.ThrowIfNull(engine);
        Init(engine, options, ownsEngine);
    }

    private void Init(IOcrEngine? engine, ExtractorOptions? options, bool ownsEngine)
    {
        _options = options ?? new ExtractorOptions();
        var resolved = engine ?? _options.Engine;
        _ownedEngine = ownsEngine ? resolved : null;
        _pipeline = new ExtractionPipeline(_options, resolved);
    }

    /// <summary>
    /// Распознать весь документ (PDF / Office / изображение).
    /// </summary>
    public Task<DocumentResult> ExtractAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        return _pipeline.ExtractAsync(source, cancellationToken);
    }

    /// <summary>
    /// Распознать выбранный сегмент страницы. Координаты результата — в пространстве полной страницы.
    /// </summary>
    public Task<RegionResult> ExtractRegionAsync(
        DocumentSource source,
        PageRegion region,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(region);
        return _pipeline.ExtractRegionAsync(source, region, cancellationToken);
    }

    /// <summary>
    /// Зарезервировано: извлечение типизированных полей по JSON-схеме и визуальным зонам (этап 1).
    /// </summary>
    public Task<FieldExtractionResult> ExtractFieldsAsync(
        DocumentSource source,
        FieldSchema schema,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(schema);
        return Task.FromException<FieldExtractionResult>(new NotSupportedException(
            "Извлечение структурированных полей будет доступно на этапе 1. См. docs/ROADMAP.md."));
    }

    public Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        return _pipeline.GetPageCountAsync(source, cancellationToken);
    }

    public Task<PageImage> RenderPageAsync(
        DocumentSource source,
        int pageIndex,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        return _pipeline.RenderPageAsync(source, pageIndex, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ownedEngine?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownedEngine is not null)
        {
            await _ownedEngine.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
