using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServer.Protocol;

public sealed class LspResult
{
    public LspResult(LspResponse? response, IReadOnlyList<LspNotification> notifications, bool shutdown)
    {
        Response = response;
        Notifications = notifications ?? Array.Empty<LspNotification>();
        Shutdown = shutdown;
    }

    public LspResponse? Response { get; }
    public IReadOnlyList<LspNotification> Notifications { get; }
    public bool Shutdown { get; }

    public static LspResult Empty { get; } = new(null, Array.Empty<LspNotification>(), false);
}
