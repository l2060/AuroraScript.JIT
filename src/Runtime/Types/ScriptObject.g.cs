using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System;
using System.Security.Cryptography;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial class for <see cref="ScriptObject"/> containing foundational native method implementations.
    /// Includes global constants and basic utility methods like toString and property count.
    /// </summary>
    public partial class ScriptObject
    {
        /// <summary> Represents the global 'null' script object instance. </summary>
        public static readonly ScriptObject Null = NullValue.Instance;

        /// <summary> Native implementation for the 'length' property of generic objects (returns property count). </summary>
        internal static void LENGTH(ScriptObject thisObject, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsNumber(ref result, thisObject.hiddenClass.PropertyCount);
        }

        /// <summary> Native implementation for the base object 'toString' method. </summary>
        internal static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, thisObject.ToString());
        }
    }
}
