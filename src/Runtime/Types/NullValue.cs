using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the 'null' value in AuroraScript.
    /// This is a specialized object that throws exceptions when property access is attempted.
    /// </summary>
    public sealed class NullValue : ScriptObject
    {
        private static readonly StringValue valueString = new StringValue("null");

        /// <summary> The global singleton instance of Null. </summary>
        public readonly static NullValue Instance = new NullValue();

        private NullValue() : base(Prototypes.NullValuePrototype)
        {
            Frozen();
        }

        /// <summary> Overridden to throw an exception as null cannot be invoked. </summary>
        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum[] args)
        {
            throw new AuroraRuntimeException("cannot be called of null.");
        }

        /// <summary> Native implementation for null.toString(). </summary>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, valueString);
        }

        /// <summary> Throws Exception: Cannot read properties of null. </summary>
        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, string key)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        internal sealed override ScriptDatum GetPropertyDatum(ScriptContext ctx, string key)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        /// <summary> Throws Exception: Cannot set properties of null. </summary>
        internal sealed override void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        internal sealed override void SetPropertyDatum(ScriptContext ctx, string key, ScriptDatum value)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        /// <summary> Throws Exception: Cannot define properties on null. </summary>
        public override void Define(string key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        /// <summary> Throws Exception: Cannot delete properties of null. </summary>
        internal sealed override bool DeletePropertyValue(ScriptContext ctx, string key)
        {
            throw new AuroraRuntimeException(string.Format("Cannot read properties of undefined (reading '{0}')", key));
        }

        /// <summary> Returns "NULL_VALUE" for debugging. </summary>
        public override string ToString()
        {
            return "NULL_VALUE";
        }

        /// <summary> Always returns false for null. </summary>
        public override bool IsTrue()
        {
            return false;
        }
    }
}
