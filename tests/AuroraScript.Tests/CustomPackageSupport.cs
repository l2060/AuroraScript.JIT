using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Tests;

[NativeType("custom")]
[NativePackage("custom")]
public sealed partial class CustomPackageSupport : ScriptObject
{
    [Export("answer")]
    public static readonly double Answer = 42;
}
