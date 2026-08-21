using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class AstarExampleTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OptimizedAstarPreservesSearchBehavior(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var source = File.ReadAllText(FindRepositoryFile("examples", "tests", "astar.as"));
        source = source[..source.IndexOf("// examples", StringComparison.Ordinal)] +
            """

            export func verifyOptimizedAstar() {
                var openMap = new Int8Array(25);
                openMap.fill(1);
                var finder = createAStar(5, 5, openMap, null);
                var path = newPathBuffer(finder);
                var diagonalCount = findPathInto(finder, 0, 0, 4, 4, path, true, true);
                var diagonalOk = diagonalCount == 5 && path[0] == 0 && path[4] == 24;

                var cornerMap = new Int8Array(4);
                cornerMap.fill(1);
                cornerMap[1] = 0;
                cornerMap[2] = 0;
                var cornerFinder = createAStar(2, 2, cornerMap, null);
                var cornerPath = newPathBuffer(cornerFinder);
                var blockedCorner = findPathInto(cornerFinder, 0, 0, 1, 1, cornerPath, true, true);
                var allowedCorner = findPathInto(cornerFinder, 0, 0, 1, 1, cornerPath, true, false);

                var weightedMap = new Int8Array(6);
                weightedMap.fill(1);
                var weights = new Float64Array(6);
                weights.fill(1);
                weights[1] = 100;
                var weightedFinder = createAStar(3, 2, weightedMap, weights);
                var weightedPath = newPathBuffer(weightedFinder);
                var weightedCount = findPathInto(weightedFinder, 0, 0, 2, 0, weightedPath, false, true);
                var avoidsExpensiveCell = weightedCount == 5 && weightedPath[1] == 3 &&
                    weightedPath[2] == 4 && weightedPath[3] == 5;

                return [
                    diagonalOk,
                    blockedCorner,
                    allowedCorner,
                    avoidsExpensiveCell,
                    weightedFinder.expanded > 0
                ];
            }
            """;

        var (_, domain) = await workspace.CompileModuleAsync(source, mode);
        using (domain)
        {
            ScriptAssert.Equal(
                new object?[] { true, 0, 2, true, true },
                TestWorkspace.Execute(domain, "verifyOptimizedAstar", "ASTAR"));
        }
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
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            "Could not locate repository file: " + Path.Combine(segments));
    }
}
