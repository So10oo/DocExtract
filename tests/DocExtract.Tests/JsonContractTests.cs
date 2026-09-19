using System.Text.Json;

namespace DocExtract.Tests;

public class JsonContractTests
{
    [Fact]
    public void DocumentResult_Json_HasSchemaVersion()
    {
        var result = new DocumentResult
        {
            Source = new DocumentMetadata { FileName = "a.pdf", PageCount = 1 },
            Options = new ExtractorOptionsSnapshot
            {
                Languages = ["ru", "en"],
                PreferEmbeddedText = true,
                Gpu = GpuPreference.None,
                RenderDpi = 200
            },
            Pages =
            [
                new PageResult
                {
                    PageIndex = 0,
                    PixelWidth = 100,
                    PixelHeight = 200,
                    Dpi = 200,
                    SourceKind = PageSourceKind.EmbeddedText,
                    Text = "Hello",
                    Blocks = []
                }
            ]
        };

        var json = result.ToJson("corrected");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("corrected", doc.RootElement.GetProperty("correctedText").GetString());
        Assert.Equal("Hello", doc.RootElement.GetProperty("pages")[0].GetProperty("text").GetString());
        Assert.Equal("embeddedText", doc.RootElement.GetProperty("pages")[0].GetProperty("sourceKind").GetString());
    }

    [Fact]
    public void ZoneTemplate_RoundTrip()
    {
        var template = new ZoneTemplate
        {
            Name = "invoice",
            Zones =
            [
                new TemplateZone
                {
                    Name = "number",
                    PageIndex = 0,
                    NormalizedBounds = Box.Normalized(0.1, 0.05, 0.2, 0.04),
                    FieldType = FieldValueType.String
                }
            ]
        };

        var json = template.ToJson();
        var back = ZoneTemplate.FromJson(json);
        Assert.Equal("invoice", back.Name);
        Assert.Single(back.Zones);
        Assert.Equal("number", back.Zones[0].Name);
        Assert.Equal(0.1, back.Zones[0].NormalizedBounds.X, 5);
    }

    [Fact]
    public void FieldSchema_RoundTrip()
    {
        var schema = new FieldSchema
        {
            Fields =
            [
                new FieldDefinition
                {
                    Name = "date",
                    Type = FieldValueType.Date,
                    Zone = new PageRegion(0, Box.Normalized(0.2, 0.2, 0.3, 0.05))
                }
            ]
        };
        var back = FieldSchema.FromJson(schema.ToJson());
        Assert.Equal("date", back.Fields[0].Name);
        Assert.Equal(FieldValueType.Date, back.Fields[0].Type);
    }
}
