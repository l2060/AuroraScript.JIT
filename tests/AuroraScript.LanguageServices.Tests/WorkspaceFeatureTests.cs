using AuroraScript.LanguageServices.Workspace;
using System;
using System.IO;

namespace AuroraScript.LanguageServices.Tests;

public sealed class WorkspaceFeatureTests
{
    [Fact]
    public void WorkspaceVersionChangesOnlyWhenDocumentsChange()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-workspace-" + Guid.NewGuid().ToString("N"));
        var workspace = new AuroraWorkspace(root);
        var path = Path.Combine(root, "main.as");

        workspace.OpenOrUpdate(path, "@module(TEST);", version: 1);
        var firstVersion = workspace.Version;
        workspace.OpenOrUpdate(path, "@module(TEST);", version: 1);
        var secondVersion = workspace.Version;
        workspace.OpenOrUpdate(path, "@module(TEST); export const value = 1;", version: 2);
        var thirdVersion = workspace.Version;
        workspace.Close(path);
        var fourthVersion = workspace.Version;

        Assert.Equal(1, firstVersion);
        Assert.Equal(firstVersion, secondVersion);
        Assert.Equal(2, thirdVersion);
        Assert.Equal(3, fourthVersion);
    }
}
