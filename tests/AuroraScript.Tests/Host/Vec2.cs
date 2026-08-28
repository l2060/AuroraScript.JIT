using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Tests.Host;

[AuroraNativeObject("Vec2")]
public sealed partial class Vec2 : AuroraNativeObject
{
    [AuroraExport("x")]
    public double X;

    [AuroraExport("y")]
    public double Y;

    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    [AuroraExport("length")]
    public double LengthCore() => Math.Sqrt((X * X) + (Y * Y));

    [AuroraExport("add")]
    public Vec2 AddCore(Vec2 other) => new Vec2(X + other.X, Y + other.Y);
}
