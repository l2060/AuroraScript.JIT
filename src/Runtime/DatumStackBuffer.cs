using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// A fixed-size stack-allocated buffer for passing arguments to bonded native functions.
    /// Each call frame gets its own buffer, ensuring reentrant safety with zero heap allocation.
    /// </summary>

    [InlineArray(1)]
    internal struct DatumBuffer1
    {
        private ScriptDatum _element;
    }

    [InlineArray(2)]
    internal struct DatumBuffer2
    {
        private ScriptDatum _element;
    }

    [InlineArray(3)]
    internal struct DatumBuffer3
    {
        private ScriptDatum _element;
    }

    [InlineArray(4)]
    internal struct DatumBuffer4
    {
        private ScriptDatum _element;
    }

    [InlineArray(5)]
    internal struct DatumBuffer5
    {
        private ScriptDatum _element;
    }

    [InlineArray(6)]
    internal struct DatumBuffer6
    {
        private ScriptDatum _element;
    }

    [InlineArray(7)]
    internal struct DatumBuffer7
    {
        private ScriptDatum _element;
    }

    [InlineArray(8)]
    internal struct DatumBuffer8
    {
        private ScriptDatum _element;
    }
}
