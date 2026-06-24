using System;

namespace AuroraScript.Compiler.Backend
{
    internal readonly struct ModuleId : IEquatable<ModuleId>
    {
        public static readonly ModuleId Invalid = new(-1);

        public ModuleId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(ModuleId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ModuleId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }

    internal readonly struct FunctionId : IEquatable<FunctionId>
    {
        public static readonly FunctionId Invalid = new(-1);

        public FunctionId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(FunctionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is FunctionId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }

    internal readonly struct ScopeId : IEquatable<ScopeId>
    {
        public static readonly ScopeId Invalid = new(-1);

        public ScopeId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(ScopeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ScopeId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }

    internal readonly struct SymbolId : IEquatable<SymbolId>
    {
        public static readonly SymbolId Invalid = new(-1);

        public SymbolId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(SymbolId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SymbolId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }

    internal readonly struct LocalSlotId : IEquatable<LocalSlotId>
    {
        public static readonly LocalSlotId Invalid = new(-1);

        public LocalSlotId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(LocalSlotId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is LocalSlotId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }

    internal readonly struct UpvalueSlotId : IEquatable<UpvalueSlotId>
    {
        public static readonly UpvalueSlotId Invalid = new(-1);

        public UpvalueSlotId(int value) => Value = value;

        public int Value { get; }
        public bool IsValid => Value >= 0;

        public bool Equals(UpvalueSlotId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UpvalueSlotId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : "Invalid";
    }
}
