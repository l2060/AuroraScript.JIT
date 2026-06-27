using System.Text.Json.Nodes;

namespace AuroraScript.LanguageServer.Protocol;

public sealed class LspNotification
{
    public LspNotification(string method, JsonObject parameters)
    {
        Method = method;
        Parameters = parameters;
    }

    public string Method { get; }
    public JsonObject Parameters { get; }
}
