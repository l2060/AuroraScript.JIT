using System;
using System.IO;
using System.Text;

namespace AuroraScript.Tests;

internal sealed class BackendTestWorkspace : IDisposable
{
    public BackendTestWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "aurora-backend-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string WriteSource(string relativePath, string source)
    {
        var path = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, source, Encoding.UTF8);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
