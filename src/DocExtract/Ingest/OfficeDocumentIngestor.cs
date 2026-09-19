using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocExtract.Ingest;

internal sealed class OfficeDocumentIngestor : IDocumentIngestor
{
    public bool CanHandle(DocumentSource source) => source.Extension is ".docx" or ".xlsx";

    public Task<int> GetPageCountAsync(DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var doc = IngestAsync(source, new ExtractorOptions { PreferEmbeddedText = true }, cancellationToken)
            .GetAwaiter()
            .GetResult();
        return Task.FromResult(doc.Pages.Count);
    }

    public Task<IngestedDocument> IngestAsync(DocumentSource source, ExtractorOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pages = source.Extension == ".xlsx"
            ? LoadXlsx(source, options)
            : LoadDocx(source, options);

        return Task.FromResult(new IngestedDocument
        {
            FileName = source.FileName,
            FullPath = source.FilePath,
            Pages = pages
        });
    }

    public Task<PageImage> RenderPageAsync(DocumentSource source, int pageIndex, ExtractorOptions options, CancellationToken cancellationToken)
    {
        var ingested = IngestAsync(source, options, cancellationToken).GetAwaiter().GetResult();
        if (pageIndex < 0 || pageIndex >= ingested.Pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        }

        var page = ingested.Pages[pageIndex];
        if (page.RasterPng is { Length: > 0 })
        {
            return Task.FromResult(new PageImage
            {
                PageIndex = pageIndex,
                Width = page.PixelWidth,
                Height = page.PixelHeight,
                Dpi = page.Dpi,
                PngBytes = page.RasterPng
            });
        }

        var png = RenderWordsPreview(page);
        return Task.FromResult(new PageImage
        {
            PageIndex = pageIndex,
            Width = page.PixelWidth,
            Height = page.PixelHeight,
            Dpi = page.Dpi,
            PngBytes = png
        });
    }

    private static List<IngestedPage> LoadDocx(DocumentSource source, ExtractorOptions options)
    {
        using var stream = source.OpenRead();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        var lines = new List<string>();
        if (body is not null)
        {
            foreach (var para in body.Elements<Paragraph>())
            {
                var text = para.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text.Trim());
                }
            }
        }

        var pages = new List<IngestedPage>
        {
            BuildTextPage(0, lines, options.RenderDpi)
        };

        var imageIndex = 1;
        if (doc.MainDocumentPart is not null)
        {
            foreach (var imagePart in doc.MainDocumentPart.ImageParts)
            {
                using var imageStream = imagePart.GetStream();
                using var copy = new MemoryStream();
                imageStream.CopyTo(copy);
                try
                {
                    var (w, h, png) = RasterCodec.ToPng(copy.ToArray());
                    pages.Add(new IngestedPage
                    {
                        PageIndex = imageIndex,
                        PixelWidth = w,
                        PixelHeight = h,
                        Dpi = options.RenderDpi,
                        Kind = PageSourceKind.Ocr,
                        RasterPng = png
                    });
                    imageIndex++;
                }
                catch
                {
                    // неподдерживаемый формат встроенной картинки — пропускаем
                }
            }
        }

        // перенумерация на случай пропусков
        for (var i = 0; i < pages.Count; i++)
        {
            pages[i] = new IngestedPage
            {
                PageIndex = i,
                PixelWidth = pages[i].PixelWidth,
                PixelHeight = pages[i].PixelHeight,
                Dpi = pages[i].Dpi,
                Kind = pages[i].Kind,
                RasterPng = pages[i].RasterPng,
                EmbeddedWords = pages[i].EmbeddedWords
            };
        }

        return pages;
    }

    private static List<IngestedPage> LoadXlsx(DocumentSource source, ExtractorOptions options)
    {
        using var stream = source.OpenRead();
        using var doc = SpreadsheetDocument.Open(stream, false);
        var workbook = doc.WorkbookPart ?? throw new InvalidOperationException("XLSX без WorkbookPart.");
        var shared = workbook.SharedStringTablePart?.SharedStringTable;
        var pages = new List<IngestedPage>();
        var index = 0;
        foreach (var sheet in workbook.Workbook.Sheets?.OfType<Sheet>() ?? [])
        {
            if (sheet.Id?.Value is null)
            {
                continue;
            }

            if (workbook.GetPartById(sheet.Id.Value) is not WorksheetPart wsPart)
            {
                continue;
            }

            var lines = new List<string>();
            var sheetData = wsPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData is not null)
            {
                foreach (var row in sheetData.Elements<Row>())
                {
                    var cells = row.Elements<Cell>().Select(c => GetCellText(c, shared));
                    var line = string.Join('\t', cells.Where(v => !string.IsNullOrWhiteSpace(v)));
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line);
                    }
                }
            }

            pages.Add(BuildTextPage(index, lines, options.RenderDpi, sheet.Name?.Value));
            index++;
        }

        if (pages.Count == 0)
        {
            pages.Add(BuildTextPage(0, [], options.RenderDpi));
        }

        return pages;
    }

    private static string GetCellText(Cell cell, SharedStringTable? shared)
    {
        var value = cell.CellValue?.InnerText ?? "";
        if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out var i) && shared is not null)
        {
            return shared.ElementAt(i).InnerText;
        }

        return value;
    }

    private static IngestedPage BuildTextPage(int pageIndex, IReadOnlyList<string> lines, int dpi, string? title = null)
    {
        const int width = 1240;
        const double left = 48;
        const double top = 48;
        const double lineHeight = 22;
        var words = new List<EmbeddedWord>();
        var y = top;
        if (!string.IsNullOrWhiteSpace(title))
        {
            words.Add(new EmbeddedWord
            {
                Text = title!,
                BoundingBox = new PixelBox(left, y, Math.Min(title!.Length * 10, width - left * 2), lineHeight)
            });
            y += lineHeight * 1.4;
        }

        foreach (var line in lines)
        {
            var w = Math.Min(Math.Max(40, line.Length * 8), width - left * 2);
            words.Add(new EmbeddedWord
            {
                Text = line,
                BoundingBox = new PixelBox(left, y, w, lineHeight)
            });
            y += lineHeight;
        }

        var height = Math.Max(1754, (int)Math.Ceiling(y + top));
        return new IngestedPage
        {
            PageIndex = pageIndex,
            PixelWidth = width,
            PixelHeight = height,
            Dpi = dpi,
            Kind = lines.Count == 0 ? PageSourceKind.Empty : PageSourceKind.EmbeddedText,
            EmbeddedWords = words
        };
    }

    private static byte[] RenderWordsPreview(IngestedPage page)
    {
        using var bitmap = new SkiaSharp.SKBitmap(page.PixelWidth, page.PixelHeight);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.White);
        using var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true };
        using var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 16);
        foreach (var word in page.EmbeddedWords)
        {
            canvas.DrawText(word.Text, (float)word.BoundingBox.X, (float)(word.BoundingBox.Y + 16), SkiaSharp.SKTextAlign.Left, font, paint);
        }

        return RasterCodec.Encode(bitmap).Png;
    }
}
