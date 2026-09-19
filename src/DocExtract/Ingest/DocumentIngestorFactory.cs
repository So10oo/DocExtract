namespace DocExtract.Ingest;

internal static class DocumentIngestorFactory
{
    private static readonly IDocumentIngestor[] Ingestors =
    [
        new PdfDocumentIngestor(),
        new OfficeDocumentIngestor(),
        new RasterDocumentIngestor()
    ];

    public static IDocumentIngestor For(DocumentSource source) =>
        Ingestors.FirstOrDefault(i => i.CanHandle(source))
        ?? throw new NotSupportedException($"Формат «{source.Extension}» не поддерживается. Ожидаются PDF, DOCX, XLSX, PNG, JPEG, TIFF, WebP, BMP.");
}
