using System.Text;
using DocExtract.Export;

namespace DocExtract;

/// <summary>
/// Результат одной страницы в канонической системе координат (пиксели растра + нормализованные 0..1).
/// </summary>
public sealed class PageResult
{
    public int PageIndex { get; init; }

    public int PixelWidth { get; init; }

    public int PixelHeight { get; init; }

    public int Dpi { get; init; }

    public int Rotation { get; init; }

    public PageSourceKind SourceKind { get; init; }

    public required string Text { get; init; }

    public IReadOnlyList<TextBlock> Blocks { get; init; } = [];
}

/// <summary>
/// Полный результат документа. <see cref="SchemaVersion"/> версионирует JSON-контракт.
/// </summary>
public sealed class DocumentResult
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required DocumentMetadata Source { get; init; }

    public required ExtractorOptionsSnapshot Options { get; init; }

    public IReadOnlyList<PageResult> Pages { get; init; } = [];

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Pages.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine().AppendLine("\f");
            }

            sb.Append(Pages[i].Text);
        }

        return sb.ToString();
    }

    public string ToJson(string? correctedText = null) => ResultJsonSerializer.Serialize(this, correctedText);
}

/// <summary>
/// Результат распознавания сегмента страницы. Координаты — в пространстве полной страницы.
/// </summary>
public sealed class RegionResult
{
    public int SchemaVersion { get; init; } = DocumentResult.CurrentSchemaVersion;

    public required PageRegion Region { get; init; }

    public required PageResult Page { get; init; }

    public required DocumentMetadata Source { get; init; }

    public string ToPlainText() => Page.Text;

    public string ToJson() => ResultJsonSerializer.SerializeRegion(this);
}

public sealed class DocumentMetadata
{
    public required string FileName { get; init; }

    public string? FullPath { get; init; }

    public int PageCount { get; init; }
}

public sealed class ExtractorOptionsSnapshot
{
    public IReadOnlyList<string> Languages { get; init; } = [];

    public bool PreferEmbeddedText { get; init; }

    public GpuPreference Gpu { get; init; }

    public int RenderDpi { get; init; }
}

/// <summary>
/// Растр страницы для превью в десктопе и у потребителей SDK.
/// </summary>
public sealed class PageImage
{
    public int PageIndex { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public int Dpi { get; init; }

    public required byte[] PngBytes { get; init; }
}
