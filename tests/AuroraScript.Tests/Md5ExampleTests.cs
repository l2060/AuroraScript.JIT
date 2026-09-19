using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class Md5ExampleTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OptimizedMd5PreservesExistingResults(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var source = File.ReadAllText(FindRepositoryFile("examples", "tests", "md5.as"));
        workspace.WriteSource("md5.as", source);

        var engine = workspace.CreateEngine(mode, assemblyOut: mode == CompilationMode.Persistence
            ? Path.Combine(workspace.Root, "test-output.dll") : null);
        await engine.BuildAsync(["md5.as"]);
        using var domain = engine.CreateDomain();

        AssertHash(domain, string.Empty, "d18d800b04e8099ef47");
        AssertHash(domain, "12345", "87c0ee87643a69f47");
        AssertHash(domain, "AuroraScript", "6b30c036a3cb25f3db");
        AssertHash(domain, "line1\r\nline2", "e55024156b3c5d5e1");
        AssertHash(domain, "中文", "abc29cc306703d077407");
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var method = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "MD5$native");
            var locals = method.GetMethodBody()!.LocalVariables;
            Assert.DoesNotContain(locals, local => local.LocalType == typeof(double));
            Assert.Contains(locals, local => local.LocalType == typeof(long));
            var calls = StringOptimizationTests.GetCalls(method);
            Assert.DoesNotContain(calls, call => call.Name is "CharCodeAtCore" or "ValidateLength" or "ToArithmeticNumber");
            Assert.Contains(calls, call => call.DeclaringType == typeof(string) && call.Name == "get_Chars");
        }
#endif
    }

    private static void AssertHash(ScriptDomain domain, string input, string expected)
    {
        var result = TestWorkspace.Execute(
            domain,
            "MD5",
            "MD5_LIB",
            ScriptDatum.FromString(input));

        Assert.Equal(expected, result.StringText);
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            var candidate = directory.FullName;
            foreach (var segment in segments)
            {
                candidate = Path.Combine(candidate, segment);
            }
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "Could not locate repository file: " + Path.Combine(segments));
    }
}
