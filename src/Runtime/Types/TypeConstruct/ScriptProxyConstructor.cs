using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    internal class ScriptProxyConstructor : ScriptType
    {
        internal readonly static ScriptProxyConstructor INSTANCE = new ScriptProxyConstructor();

        internal ScriptProxyConstructor() : base("Proxy")
        {

        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var _object) && args.TryGetObject(1, out var options))
            {
                if (options.GetPropertyValue("get") == ScriptObject.Null || options.GetPropertyValue("set") == ScriptObject.Null)
                {
                    throw new AuroraRuntimeException("Proxy requires get and set handlers.");
                }

                ScriptProxy proxy = new ScriptProxy(_object, options);
                ScriptDatum.WriteAsObject(ref result, proxy);
            }
        }
    }
}
