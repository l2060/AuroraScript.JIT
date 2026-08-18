using AuroraScript.Runtime.Types;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    internal static class IterationOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptEnumerator GetEnumerator(ScriptDatum value) =>
            ScriptDatum.ToObject(value).GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MoveNext(ScriptEnumerator iterator, out ScriptDatum value) =>
            iterator.NextValue(out value);
    }
}
