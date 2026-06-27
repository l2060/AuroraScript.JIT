using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.LanguageServer;

internal static class Program
{
    public static async Task Main()
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();
        var server = AuroraLanguageServerFactory.CreateDefault();
        var transport = new LspStdioServer(server, input, output);
        await transport.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
