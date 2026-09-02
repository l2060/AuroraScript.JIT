using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Tests.Host;

[AuroraNativeType("Vec2")]
public sealed partial class Vec2 : ScriptObject, INativeTypedDocument
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

    public void WriteTypedDocument(ref TypedDocumentOutput output)
    {
        output.WriteElement(X);
        output.WriteElement(Y);
    }

    public void ReadTypedDocument(ref TypedDocumentInput input)
    {
        if (input.IsElement)
        {
            switch (input.ElementIndex)
            {
                case 0:
                    X = ReadFiniteNumber(ref input);
                    return;
                case 1:
                    Y = ReadFiniteNumber(ref input);
                    return;
                default:
                    throw input.Error("Vec2 array form requires exactly two numbers.");
            }
        }

        if (input.IsReadOnly)
        {
            throw input.Error("readonly is not supported by Vec2 TDoc members.");
        }

        switch (input.MemberName)
        {
            case "x":
                X = ReadFiniteNumber(ref input);
                return;
            case "y":
                Y = ReadFiniteNumber(ref input);
                return;
            default:
                throw input.Error(
                    $"Unknown field '{input.MemberName}' for native type 'Vec2'.");
        }
    }

    private static double ReadFiniteNumber(ref TypedDocumentInput input)
    {
        var value = input.Value;
        if (value.Kind != ValueKind.Number || !double.IsFinite(value.Number))
        {
            throw input.Error("Vec2 values require a finite number.");
        }

        return value.Number;
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
