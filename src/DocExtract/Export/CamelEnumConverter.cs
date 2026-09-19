using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocExtract;

public sealed class CamelEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
{
    public CamelEnumConverter() : base(JsonNamingPolicy.CamelCase)
    {
    }
}
