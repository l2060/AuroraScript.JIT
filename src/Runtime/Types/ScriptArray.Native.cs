using AuroraScript.Hosting;
using System;
using System.Text;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class ScriptArray
    {
        /// <summary>Returns the current length without appending values.</summary>
        [Export("push", DynamicAdapter = nameof(PUSH))]
        public int PushCore() => _count;

        /// <summary>Appends one value and returns the new length.</summary>
        [Export("push", DynamicAdapter = nameof(PUSH))]
        public int PushCore(ScriptDatum value)
        {
            Push(value);
            return _count;
        }

        /// <summary>Appends two values without allocating an argument array.</summary>
        [Export("push", DynamicAdapter = nameof(PUSH))]
        public int PushCore(ScriptDatum first, ScriptDatum second)
        {
            var index = _count;
            EnsureCapacity(checked(index + 2));
            _items[index] = first;
            _items[index + 1] = second;
            return _count = index + 2;
        }

        /// <summary>Appends three values without allocating an argument array.</summary>
        [Export("push", DynamicAdapter = nameof(PUSH))]
        public int PushCore(ScriptDatum first, ScriptDatum second, ScriptDatum third)
        {
            var index = _count;
            EnsureCapacity(checked(index + 3));
            _items[index] = first;
            _items[index + 1] = second;
            _items[index + 2] = third;
            return _count = index + 3;
        }

        /// <summary>Appends four values without allocating an argument array.</summary>
        [Export("push", DynamicAdapter = nameof(PUSH))]
        public int PushCore(ScriptDatum first, ScriptDatum second, ScriptDatum third, ScriptDatum fourth)
        {
            var index = _count;
            EnsureCapacity(checked(index + 4));
            _items[index] = first;
            _items[index + 1] = second;
            _items[index + 2] = third;
            _items[index + 3] = fourth;
            return _count = index + 4;
        }



        /// <summary>Sets the logical length using a native integer.</summary>
        [Export("length", IsSetter = true, DynamicAdapter = nameof(SET_LENGTH))]
        public void SetLengthCore(int value) => SetLength(value);

        private static void SET_LENGTH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length == 0 || !TryGetLength(args[0], out var length))
                throw new AuroraRuntimeException("Invalid array length.");
            ((ScriptArray)thisObject).SetLength(length);
        }

        /// <summary>Removes and returns the last value.</summary>
        [Export("pop", DynamicAdapter = nameof(POP))]
        public ScriptDatum PopCore()
        {
            ScriptDatum result = default;
            PopTo(ref result);
            return result;
        }

        /// <summary>Reverses this array in place.</summary>
        [Export("reverse", DynamicAdapter = nameof(REVERSE))]
        public ScriptArray ReverseCore()
        {
            Array.Reverse(_items, 0, _count);
            return this;
        }

        /// <summary>Sorts this array using script value ordering.</summary>
        [Export("sort", DynamicAdapter = nameof(SORT))]
        public ScriptArray SortCore()
        {
            Array.Sort(_items, 0, _count, CompareDatumForSort);
            return this;
        }

        /// <summary>Removes and returns the first value.</summary>
        [Export("shift", DynamicAdapter = nameof(SHIFT))]
        public ScriptDatum ShiftCore()
        {
            if (_count == 0) return default;
            var result = _items[0];
            Array.Copy(_items, 1, _items, 0, --_count);
            _items[_count] = default;
            return result;
        }

        /// <summary>Handles 0 values without allocating an argument array.</summary>
        [Export("unshift", DynamicAdapter = nameof(UNSHIFT))]
        public int UnshiftCore()
        {
            return _count;
        }

        /// <summary>Handles 1 values without allocating an argument array.</summary>
        [Export("unshift", DynamicAdapter = nameof(UNSHIFT))]
        public int UnshiftCore(ScriptDatum value0)
        {
            DatumBuffer1 values = default;
            values[0] = value0;
            return UnshiftValues(values);
        }

        /// <summary>Handles 2 values without allocating an argument array.</summary>
        [Export("unshift", DynamicAdapter = nameof(UNSHIFT))]
        public int UnshiftCore(ScriptDatum value0, ScriptDatum value1)
        {
            DatumBuffer2 values = default;
            values[0] = value0;
            values[1] = value1;
            return UnshiftValues(values);
        }

        /// <summary>Handles 3 values without allocating an argument array.</summary>
        [Export("unshift", DynamicAdapter = nameof(UNSHIFT))]
        public int UnshiftCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2)
        {
            DatumBuffer3 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            return UnshiftValues(values);
        }

        /// <summary>Handles 4 values without allocating an argument array.</summary>
        [Export("unshift", DynamicAdapter = nameof(UNSHIFT))]
        public int UnshiftCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2, ScriptDatum value3)
        {
            DatumBuffer4 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            values[3] = value3;
            return UnshiftValues(values);
        }

        /// <summary>Handles 0 values without allocating an argument array.</summary>
        [Export("concat", DynamicAdapter = nameof(CONCAT))]
        public ScriptArray ConcatCore()
        {
            return new ScriptArray(this);
        }

        /// <summary>Handles 1 values without allocating an argument array.</summary>
        [Export("concat", DynamicAdapter = nameof(CONCAT))]
        public ScriptArray ConcatCore(ScriptDatum value0)
        {
            DatumBuffer1 values = default;
            values[0] = value0;
            return ConcatValues(values);
        }

        /// <summary>Handles 2 values without allocating an argument array.</summary>
        [Export("concat", DynamicAdapter = nameof(CONCAT))]
        public ScriptArray ConcatCore(ScriptDatum value0, ScriptDatum value1)
        {
            DatumBuffer2 values = default;
            values[0] = value0;
            values[1] = value1;
            return ConcatValues(values);
        }

        /// <summary>Handles 3 values without allocating an argument array.</summary>
        [Export("concat", DynamicAdapter = nameof(CONCAT))]
        public ScriptArray ConcatCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2)
        {
            DatumBuffer3 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            return ConcatValues(values);
        }

        /// <summary>Handles 4 values without allocating an argument array.</summary>
        [Export("concat", DynamicAdapter = nameof(CONCAT))]
        public ScriptArray ConcatCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2, ScriptDatum value3)
        {
            DatumBuffer4 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            values[3] = value3;
            return ConcatValues(values);
        }

        private int UnshiftValues(Span<ScriptDatum> values)
        {
            var count = values.Length;
            EnsureCapacity(_count + count);
            Array.Copy(_items, 0, _items, count, _count);
            values.CopyTo(_items);
            _count += count;
            return _count;
        }

        private ScriptArray ConcatValues(Span<ScriptDatum> values)
        {
            var result = new ScriptArray(this);
            foreach (var value in values)
            {
                if (ScriptDatum.TryGetArray(in value, out var array))
                    result.AddRange(array.Values());
                else result.Push(value);
            }
            return result;
        }

        /// <summary>Joins values using a separator.</summary>
        [Export("join", DynamicAdapter = nameof(JOIN))]
        public string JoinCore() => JoinCore(string.Empty);

        /// <summary>Joins values using the script separator conversion.</summary>
        private string JoinCore(ScriptDatum separator)
        {
            DatumBuffer1 args = default;
            args[0] = separator;
            ((Span<ScriptDatum>)args).TryGetString(0, out var text);
            return JoinCore(text);
        }

        /// <summary>Joins values using a native string separator.</summary>
        [Export("join", DynamicAdapter = nameof(JOIN))]
        public string JoinCore(string text)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _count; i++)
            {
                if (i > 0) builder.Append(text);
                if (_items[i].Kind != ValueKind.Null) builder.Append(ScriptDatum.ToString(_items[i]));
            }
            return builder.ToString();
        }

        /// <summary>Returns this array when no slice bounds are supplied.</summary>
        [Export("slice", DynamicAdapter = nameof(SLICE))]
        public ScriptArray SliceCore() => this;

        /// <summary>Copies a range, with negative bounds and script conversions.</summary>
        private ScriptDatum SliceCore(ScriptDatum start, ScriptDatum end = default)
        {
            ScriptDatum.TryToInteger(in start, out var from);
            var result = default(ScriptDatum);
            if (ScriptDatum.TryToInteger(in end, out var to)) SliceTo((int)from, (int)to, ref result);
            else SliceTo((int)from, ref result);
            return result;
        }

        /// <summary>Copies a range using native indices.</summary>
        [Export("slice", DynamicAdapter = nameof(SLICE))]
        public ScriptDatum SliceCore(int start)
        {
            ScriptDatum result = default;
            SliceTo(start, ref result);
            return result;
        }

        /// <summary>Copies a range using native indices.</summary>
        [Export("slice", DynamicAdapter = nameof(SLICE))]
        public ScriptDatum SliceCore(int start, int end)
        {
            ScriptDatum result = default;
            SliceTo(start, end, ref result);
            return result;
        }

        /// <summary>Runs indexOf with native control arguments.</summary>
        [Export("indexOf", DynamicAdapter = nameof(INDEXOF))]
        public int IndexOfCore(ScriptDatum value, int from = 0) => IndexOf(in value, from);

        /// <summary>Runs lastIndexOf with native control arguments.</summary>
        [Export("lastIndexOf", DynamicAdapter = nameof(LASTINDEXOF))]
        public int LastIndexOfCore(ScriptDatum value) => LastIndexOf(in value, null);

        /// <summary>Runs lastIndexOf with native control arguments.</summary>
        [Export("lastIndexOf", DynamicAdapter = nameof(LASTINDEXOF))]
        public int LastIndexOfCore(ScriptDatum value, int from) => LastIndexOf(in value, from);

        /// <summary>Runs flat with native control arguments.</summary>
        [Export("flat", DynamicAdapter = nameof(FLAT))]
        public ScriptArray FlatCore(int depth = 1) => FlatInternal(depth);

        /// <summary>Runs map with a native callback.</summary>
        [Export("map", DynamicAdapter = nameof(MAP))]
        public ScriptArray MapCore(ScriptContext ctx, ClosureFunction callback) => MapInternal(ctx, callback);

        /// <summary>Runs filter with a native callback.</summary>
        [Export("filter", DynamicAdapter = nameof(FILTER))]
        public ScriptArray FilterCore(ScriptContext ctx, ClosureFunction callback) => FilterInternal(ctx, callback);

        /// <summary>Runs find with a native callback.</summary>
        [Export("find", DynamicAdapter = nameof(FIND))]
        public ScriptDatum FindCore(ScriptContext ctx, ClosureFunction callback) => FindInternal(ctx, callback);

        /// <summary>Runs findIndex with a native callback.</summary>
        [Export("findIndex", DynamicAdapter = nameof(FINDINDEX))]
        public int FindIndexCore(ScriptContext ctx, ClosureFunction callback) => FindIndexInternal(ctx, callback);

        /// <summary>Runs findLast with a native callback.</summary>
        [Export("findLast", DynamicAdapter = nameof(FINDLAST))]
        public ScriptDatum FindLastCore(ScriptContext ctx, ClosureFunction callback) => FindLastInternal(ctx, callback);

        /// <summary>Runs findLastIndex with a native callback.</summary>
        [Export("findLastIndex", DynamicAdapter = nameof(FINDLASTINDEX))]
        public int FindLastIndexCore(ScriptContext ctx, ClosureFunction callback) => FindLastIndexInternal(ctx, callback);

        /// <summary>Runs some with a native callback.</summary>
        [Export("some", DynamicAdapter = nameof(SOME))]
        public bool SomeCore(ScriptContext ctx, ClosureFunction callback) => SomeInternal(ctx, callback);

        /// <summary>Runs every with a native callback.</summary>
        [Export("every", DynamicAdapter = nameof(EVERY))]
        public bool EveryCore(ScriptContext ctx, ClosureFunction callback) => EveryInternal(ctx, callback);

        /// <summary>Runs reduce with a native callback.</summary>
        [Export("reduce", DynamicAdapter = nameof(REDUCE))]
        public ScriptDatum ReduceCore(ScriptContext ctx, ClosureFunction callback) => ReduceInternal(ctx, callback);

        /// <summary>Tests whether a value is present.</summary>
        [Export("has", DynamicAdapter = nameof(HAS))]
        public bool HasCore(ScriptDatum value) => Has(in value);

        /// <summary>Finds the first matching value.</summary>
        private int IndexOfCore(ScriptDatum value, ScriptDatum from = default) =>
            IndexOf(in value, TryGetNativeInt32(from, out var index) ? index : null);

        /// <summary>Finds the last matching value.</summary>
        private int LastIndexOfCore(ScriptDatum value, ScriptDatum from = default) =>
            LastIndexOf(in value, TryGetNativeInt32(from, out var index) ? index : null);

        /// <summary>Flattens nested arrays to the requested depth.</summary>
        private ScriptArray FlatCore(ScriptDatum depth = default) =>
            FlatInternal(TryGetNativeInt32(depth, out var count) ? count : 1);

        private static bool TryGetNativeInt32(ScriptDatum value, out int result)
        {
            DatumBuffer1 args = default;
            args[0] = value;
            return ((Span<ScriptDatum>)args).TryGetInt32(0, out result);
        }

        /// <summary>Runs the map callback operation.</summary>
        private ScriptArray MapCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? MapInternal(ctx, function) : default;

        /// <summary>Runs the filter callback operation.</summary>
        private ScriptArray FilterCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? FilterInternal(ctx, function) : default;

        /// <summary>Runs the find callback operation.</summary>
        private ScriptDatum FindCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? FindInternal(ctx, function) : default;

        /// <summary>Runs the findIndex callback operation.</summary>
        private ScriptDatum FindIndexCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? ScriptDatum.FromNumber(FindIndexInternal(ctx, function)) : default;

        /// <summary>Runs the findLast callback operation.</summary>
        private ScriptDatum FindLastCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? FindLastInternal(ctx, function) : default;

        /// <summary>Runs the findLastIndex callback operation.</summary>
        private ScriptDatum FindLastIndexCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? ScriptDatum.FromNumber(FindLastIndexInternal(ctx, function)) : default;

        /// <summary>Runs the some callback operation.</summary>
        private ScriptDatum SomeCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? ScriptDatum.FromBoolean(SomeInternal(ctx, function)) : default;

        /// <summary>Runs the every callback operation.</summary>
        private ScriptDatum EveryCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? ScriptDatum.FromBoolean(EveryInternal(ctx, function)) : default;

        /// <summary>Runs the reduce callback operation.</summary>
        private ScriptDatum ReduceCore(ScriptContext ctx, ScriptDatum callback) =>
            ScriptDatum.TryGetFunction(in callback, out var function) ? ReduceInternal(ctx, function) : default;
    }
}
