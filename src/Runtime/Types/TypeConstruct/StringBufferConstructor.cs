using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    internal class StringBufferConstructor : ScriptType
    {
        internal static StringBufferConstructor INSTANCE = new StringBufferConstructor();

        internal StringBufferConstructor() : base("StringBuffer")
        {

        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var initialValue))
            {
                ScriptDatum.WriteAsObject(ref result, new StringBuffer(initialValue));
            }
            else
            {
                ScriptDatum.WriteAsObject(ref result, new StringBuffer());
            }
        }
    }
}
