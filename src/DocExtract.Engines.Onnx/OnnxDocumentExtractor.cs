namespace DocExtract.Engines.Onnx;

/// <summary>
/// Фабрика экстрактора с локальным ONNX-движком.
/// </summary>
public static class OnnxDocumentExtractor
{
    public static DocumentExtractor Create(ExtractorOptions? options = null)
    {
        options ??= new ExtractorOptions();
        var engine = OnnxOcrEngine.Create(options);
        return new DocumentExtractor(engine, options, ownsEngine: true);
    }
}
