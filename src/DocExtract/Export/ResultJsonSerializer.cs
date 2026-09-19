using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocExtract.Export;

internal static class ResultJsonSerializer
{
    public static string Serialize(DocumentResult result, string? correctedText)
    {
        var dto = Map(result, correctedText);
        return JsonSerializer.Serialize(dto, DocExtractJsonContext.Default.DocumentResultDto);
    }

    public static string SerializeRegion(RegionResult result)
    {
        var dto = new RegionResultDto
        {
            SchemaVersion = result.SchemaVersion,
            Source = MapMeta(result.Source),
            Region = new RegionDto
            {
                PageIndex = result.Region.PageIndex,
                NormalizedBounds = MapBox(result.Region.NormalizedBounds)
            },
            Page = MapPage(result.Page)
        };
        return JsonSerializer.Serialize(dto, DocExtractJsonContext.Default.RegionResultDto);
    }

    internal static DocumentResultDto Map(DocumentResult result, string? correctedText) => new()
    {
        SchemaVersion = result.SchemaVersion,
        Source = MapMeta(result.Source),
        Options = result.Options,
        CorrectedText = correctedText,
        Pages = result.Pages.Select(MapPage).ToArray()
    };

    private static DocumentMetadataDto MapMeta(DocumentMetadata meta) => new()
    {
        FileName = meta.FileName,
        FullPath = meta.FullPath,
        PageCount = meta.PageCount
    };

    private static PageResultDto MapPage(PageResult page) => new()
    {
        PageIndex = page.PageIndex,
        PixelWidth = page.PixelWidth,
        PixelHeight = page.PixelHeight,
        Dpi = page.Dpi,
        Rotation = page.Rotation,
        SourceKind = page.SourceKind,
        Text = page.Text,
        Blocks = page.Blocks.Select(MapBlock).ToArray()
    };

    private static TextBlockDto MapBlock(TextBlock block) => new()
    {
        Text = block.Text,
        Confidence = block.Confidence,
        BoundingBox = MapPixel(block.BoundingBox),
        NormalizedBox = MapBox(block.NormalizedBox),
        Lines = block.Lines.Select(MapLine).ToArray()
    };

    private static TextLineDto MapLine(TextLine line) => new()
    {
        Text = line.Text,
        Confidence = line.Confidence,
        BoundingBox = MapPixel(line.BoundingBox),
        NormalizedBox = MapBox(line.NormalizedBox),
        Words = line.Words.Select(w => new TextWordDto
        {
            Text = w.Text,
            Confidence = w.Confidence,
            BoundingBox = MapPixel(w.BoundingBox),
            NormalizedBox = MapBox(w.NormalizedBox)
        }).ToArray()
    };

    private static PixelBoxDto MapPixel(PixelBox box) => new()
    {
        X = Round(box.X),
        Y = Round(box.Y),
        Width = Round(box.Width),
        Height = Round(box.Height)
    };

    private static BoxDto MapBox(Box box) => new()
    {
        X = Round(box.X, 6),
        Y = Round(box.Y, 6),
        Width = Round(box.Width, 6),
        Height = Round(box.Height, 6)
    };

    private static double Round(double v, int digits = 2) => Math.Round(v, digits);
}

internal sealed class DocumentResultDto
{
    public int SchemaVersion { get; set; }
    public DocumentMetadataDto Source { get; set; } = new();
    public ExtractorOptionsSnapshot Options { get; set; } = new();
    public string? CorrectedText { get; set; }
    public PageResultDto[] Pages { get; set; } = [];
}

internal sealed class RegionResultDto
{
    public int SchemaVersion { get; set; }
    public DocumentMetadataDto Source { get; set; } = new();
    public RegionDto Region { get; set; } = new();
    public PageResultDto Page { get; set; } = new();
}

internal sealed class DocumentMetadataDto
{
    public string FileName { get; set; } = "";
    public string? FullPath { get; set; }
    public int PageCount { get; set; }
}

internal sealed class RegionDto
{
    public int PageIndex { get; set; }
    public BoxDto NormalizedBounds { get; set; } = new();
}

internal sealed class PageResultDto
{
    public int PageIndex { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public int Dpi { get; set; }
    public int Rotation { get; set; }
    public PageSourceKind SourceKind { get; set; }
    public string Text { get; set; } = "";
    public TextBlockDto[] Blocks { get; set; } = [];
}

internal sealed class TextBlockDto
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public PixelBoxDto BoundingBox { get; set; } = new();
    public BoxDto NormalizedBox { get; set; } = new();
    public TextLineDto[] Lines { get; set; } = [];
}

internal sealed class TextLineDto
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public PixelBoxDto BoundingBox { get; set; } = new();
    public BoxDto NormalizedBox { get; set; } = new();
    public TextWordDto[] Words { get; set; } = [];
}

internal sealed class TextWordDto
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public PixelBoxDto BoundingBox { get; set; } = new();
    public BoxDto NormalizedBox { get; set; } = new();
}

internal sealed class PixelBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

internal sealed class BoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
