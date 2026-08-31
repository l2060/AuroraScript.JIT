using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Tests.Host;

[AuroraNativeType("Vec2")]
public sealed partial class Vec2 : ScriptObject
{
    private double _factoryValue;

    [AuroraExport("DIMENSIONS")]
    public static readonly double Dimensions = 2;

    [AuroraExport("x")]
    public double X;

    [AuroraExport("y")]
    public double Y;

    [AuroraExport]
    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    [AuroraExport("length")]
    public double LengthCore() => Math.Sqrt((X * X) + (Y * Y));

    [AuroraExport("length")]
    public static double StaticLengthCore(double x, double y) =>
        Math.Sqrt((x * x) + (y * y));

    [AuroraExport("from")]
    public static Vec2 FromCore(double x, double y)
    {
        var result = new Vec2(x, y);
        result._factoryValue = x + y;
        return result;
    }

    [AuroraExport("factoryValue")]
    public double FactoryValueCore() => _factoryValue;

    [AuroraExport("add")]
    public Vec2 AddCore(Vec2 other) => new Vec2(X + other.X, Y + other.Y);
}
