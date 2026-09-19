using System.Text.Json;
using System.Text.Json.Serialization;
using DocExtract.Export;

namespace DocExtract;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DocumentResultDto))]
[JsonSerializable(typeof(RegionResultDto))]
[JsonSerializable(typeof(FieldSchema))]
[JsonSerializable(typeof(FieldExtractionResult))]
[JsonSerializable(typeof(ZoneTemplate))]
[JsonSerializable(typeof(ExtractorOptionsSnapshot))]
internal partial class DocExtractJsonContext : JsonSerializerContext;
