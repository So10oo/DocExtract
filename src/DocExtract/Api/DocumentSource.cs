namespace DocExtract;

/// <summary>
/// Источник документа: файл на диске или буфер в памяти.
/// Поток копируется сразу, чтобы ingest и превью могли читать его повторно.
/// </summary>
public sealed class DocumentSource : IDisposable
{
    private readonly byte[]? _bytes;
    private bool _disposed;

    private DocumentSource(string fileName, string? filePath, byte[]? bytes)
    {
        FileName = fileName;
        FilePath = filePath;
        _bytes = bytes;
        Extension = Path.GetExtension(fileName).ToLowerInvariant();
    }

    public string FileName { get; }

    public string? FilePath { get; }

    public string Extension { get; }

    public static DocumentSource FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            throw new FileNotFoundException("Файл документа не найден.", full);
        }

        return new DocumentSource(Path.GetFileName(full), full, bytes: null);
    }

    public static DocumentSource FromStream(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return new DocumentSource(fileName, filePath: null, copy.ToArray());
    }

    public static DocumentSource FromBytes(byte[] bytes, string fileName)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new DocumentSource(fileName, filePath: null, bytes);
    }

    internal Stream OpenRead()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (FilePath is not null)
        {
            return File.OpenRead(FilePath);
        }

        return new MemoryStream(_bytes!, writable: false);
    }

    internal byte[] ReadAllBytes()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return FilePath is not null ? File.ReadAllBytes(FilePath) : _bytes!;
    }

    public void Dispose() => _disposed = true;
}
