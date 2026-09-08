using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Conversion = AuroraScript.Compiler.Backend.Code.TypedRuntimeMetadata.PackedBoundaryConversion;

namespace AuroraScript.Tests;

public sealed class PackedBoundaryOptimizationTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return ["Int32", typeof(int), typeof(ScriptInt32Array)];
        yield return ["Int8", typeof(sbyte), typeof(ScriptInt8Array)];
        yield return ["Float32", typeof(float), typeof(ScriptFloat32Array)];
        yield return ["Float64", typeof(double), typeof(ScriptFloat64Array)];
        yield return ["Boolean", typeof(bool), typeof(ScriptBooleanArray)];
        yield return ["UInt8", typeof(byte), typeof(ScriptUInt8Array)];
        yield return ["Int16", typeof(short), typeof(ScriptInt16Array)];
        yield return ["UInt16", typeof(ushort), typeof(ScriptUInt16Array)];
        yield return ["UInt32", typeof(uint), typeof(ScriptUInt32Array)];
        yield return ["Int64", typeof(long), typeof(ScriptInt64Array)];
        yield return ["UInt64", typeof(ulong), typeof(ScriptUInt64Array)];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryBoundarySignaturePreservesNullStorageAndIdentity(string name, Type elementType, Type wrapperType)
    {
        var kind = Enum.Parse<FlowValueType>(name + "Array");
        var wrapper = (ScriptPackedArray)Activator.CreateInstance(wrapperType, 3)!;
        wrapper.Define("tag", ScriptDatum.FromString("kept"));
        var datum = ScriptDatum.FromObject(wrapper);
        var toStorage = Resolve(kind, wrapperType, Conversion.ToStorage, "To" + name + "Storage");
        var datumToStorage = Resolve(kind, typeof(ScriptDatum), Conversion.ToStorage, "To" + name + "Storage");
        var datumToWrapper = Resolve(kind, typeof(ScriptDatum), Conversion.ToWrapper, "To" + name + "Array");
        var objectToWrapper = Resolve(kind, typeof(ScriptObject), Conversion.ToWrapper, "To" + name + "Array");
        var fromStorage = Resolve(kind, elementType.MakeArrayType(), Conversion.FromStorage, "From" + name + "Storage");
        var storage = toStorage.Invoke(null, [wrapper]);
        Assert.Same(storage, datumToStorage.Invoke(null, [datum]));
        Assert.Same(wrapper, datumToWrapper.Invoke(null, [datum]));
        Assert.Same(wrapper, objectToWrapper.Invoke(null, [wrapper]));
        var roundTrip = (ScriptDatum)fromStorage.Invoke(null, [storage])!;
        Assert.Same(wrapper, roundTrip.Object);
        Assert.Equal("kept", roundTrip.Object.GetPropertyValue("tag").ToString());
        Assert.Null(toStorage.Invoke(null, [null]));
        Assert.Null(datumToStorage.Invoke(null, [ScriptDatum.Null]));
        Assert.Null(datumToWrapper.Invoke(null, [ScriptDatum.Null]));
        Assert.Null(objectToWrapper.Invoke(null, [null]));
        Assert.Null(objectToWrapper.Invoke(null, [ScriptObject.Null]));
        Assert.Equal(ValueKind.Null, ((ScriptDatum)fromStorage.Invoke(null, [null])!).Kind);
        var fresh = Array.CreateInstance(elementType, 2);
        var created = (ScriptDatum)fromStorage.Invoke(null, [fresh])!;
        Assert.Same(fresh, toStorage.Invoke(null, [created.Object]));
        Assert.Same(created.Object, ((ScriptDatum)fromStorage.Invoke(null, [fresh])!).Object);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CheckedDatumBoundaryKeepsLegacyErrors(string name, Type elementType, Type wrapperType)
    {
        var check = typeof(TypeCheckOps).GetMethod("Check" + name + "Array")!;
        var conversion = typeof(PackedArrayBoundaryOps).GetMethod("To" + name + "Array", [typeof(ScriptDatum)])!;
        var target = (ScriptObject)Activator.CreateInstance(wrapperType, 0)!;
        var inputs = new List<ScriptDatum>
        {
            default, ScriptDatum.True, ScriptDatum.FromNumber(1), ScriptDatum.FromInt64(1),
            ScriptDatum.FromString("bad"), ScriptDatum.FromObject(new ScriptObject()),
            ScriptDatum.FromArray(new ScriptArray()), ScriptDatum.FromObject(target),
            new ScriptDatum { Kind = ValueKind.Object }
        };
        foreach (var data in Cases()) inputs.Add(ScriptDatum.FromObject((ScriptObject)Activator.CreateInstance((Type)data[2], 0)!));
        foreach (var input in inputs)
        {
            ScriptObject? expected = null;
            ScriptObject? actual = null;
            var before = Record.Exception(() => expected = input.Kind == ValueKind.Null ? null : ((ScriptDatum)check.Invoke(null, [input])!).Object);
            var after = Record.Exception(() => actual = (ScriptObject?)conversion.Invoke(null, [input]));
            var beforeCause = before is TargetInvocationException a ? a.InnerException : before;
            var afterCause = after is TargetInvocationException b ? b.InnerException : after;
            Assert.Equal(beforeCause?.GetType(), afterCause?.GetType());
            Assert.Equal(beforeCause?.Message, afterCause?.Message);
            if (before == null) Assert.Same(expected, actual);
        }
        GC.KeepAlive(elementType);
    }

    private static MethodInfo Resolve(FlowValueType kind, Type input, Conversion conversion, string methodName)
    {
        var method = TypedRuntimeMetadata.PackedArrayBoundary(kind, input, conversion);
        Assert.Equal(typeof(PackedArrayBoundaryOps).GetMethod(methodName, [input]), method);
        Parallel.For(0, 32, _ => Assert.Same(method, TypedRuntimeMetadata.PackedArrayBoundary(kind, input, conversion)));
        return method;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void WarmBoundaryPathsAllocateNothing(string name, Type elementType, Type wrapperType)
    {
        typeof(PackedBoundaryOptimizationTests).GetMethod(nameof(CheckAllocations), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(elementType, wrapperType).Invoke(null, [name]);
    }

    private static void CheckAllocations<T, TWrapper>(string name) where TWrapper : ScriptPackedArray
    {
        var kind = Enum.Parse<FlowValueType>(name + "Array");
        var toStorage = TypedRuntimeMetadata.PackedArrayBoundary(kind, typeof(TWrapper), Conversion.ToStorage)
            .CreateDelegate<Func<TWrapper, T[]>>();
        var fromStorage = TypedRuntimeMetadata.PackedArrayBoundary(kind, typeof(T[]), Conversion.FromStorage)
            .CreateDelegate<Func<T[], ScriptDatum>>();
        var wrapper = (TWrapper)Activator.CreateInstance(typeof(TWrapper), 4)!;
        var storage = toStorage(wrapper);
        ScriptDatum result = default;
        for (var i = 0; i < 30000; i++)
        {
            storage = toStorage(wrapper);
            result = fromStorage(storage);
            toStorage(null!);
            fromStorage(null!);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
        {
            storage = toStorage(wrapper);
            result = fromStorage(storage);
            toStorage(null!);
            fromStorage(null!);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Same(wrapper, result.Object);
        GC.KeepAlive(storage);
    }

    [Fact]
    public void ConcurrentBoxingPublishesOneWrapper()
    {
        var storage = new int[8];
        var results = new ScriptObject[256];
        Parallel.For(0, results.Length, i => results[i] = PackedArrayBoundaryOps.FromInt32Storage(storage).Object);
        foreach (var result in results) Assert.Same(results[0], result);
        Assert.Same(storage, PackedArrayBoundaryOps.ToInt32Storage((ScriptInt32Array)results[0]));
    }

    [Fact]
    public void IdentityCacheDoesNotKeepStorageOrWrapperAlive()
    {
        var (storage, wrapper) = CreateWeakReferences();
        for (var i = 0; i < 5 && (storage.IsAlive || wrapper.IsAlive); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.False(storage.IsAlive);
        Assert.False(wrapper.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Storage, WeakReference Wrapper) CreateWeakReferences()
    {
        var wrapper = new ScriptInt32Array(1);
        var storage = PackedArrayBoundaryOps.ToInt32Storage(wrapper);
        var roundTrip = PackedArrayBoundaryOps.FromInt32Storage(storage);
        return (new WeakReference(storage), new WeakReference(roundTrip.Object));
    }

    [Fact]
    public void MetadataCacheHitDoesNotAllocate()
    {
        var method = TypedRuntimeMetadata.PackedArrayBoundary(FlowValueType.Int32Array, typeof(ScriptDatum), Conversion.ToStorage);
        for (var i = 0; i < 10000; i++) TypedRuntimeMetadata.PackedArrayBoundary(FlowValueType.Int32Array, typeof(ScriptDatum), Conversion.ToStorage);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++) method = TypedRuntimeMetadata.PackedArrayBoundary(FlowValueType.Int32Array, typeof(ScriptDatum), Conversion.ToStorage);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        GC.KeepAlive(method);
    }
}
