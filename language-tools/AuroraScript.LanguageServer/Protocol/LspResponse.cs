using System.Text.Json.Nodes;

namespace AuroraScript.LanguageServer.Protocol;

public sealed class LspResponse
{
    public LspResponse(JsonNode? id, JsonNode? result)
    {
        Id = id;
        Result = result;
    }

    public JsonNode? Id { get; }
    public JsonNode? Result { get; }
}
