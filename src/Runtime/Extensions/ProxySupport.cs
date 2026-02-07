using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime.Extensions
{
    internal class ScriptProxyConstructor : ScriptType
    {
        internal readonly static ScriptProxyConstructor INSTANCE = new ScriptProxyConstructor();

        internal ScriptProxyConstructor() : base("Proxy")
        {
            _prototype = Prototypes.ObjectPrototype;
        }

        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var _object) && args.TryGetObject(1, out var options))
            {
                ScriptProxy proxy = new ScriptProxy(_object, options);
                ScriptDatum.WriteAsObject(ref result, proxy);
            }
        }
    }





    internal class ScriptProxy : ScriptObject
    {
        private readonly ClosureFunction _getter;
        private readonly ClosureFunction _setter;
        private readonly ClosureFunction _delete;
        private readonly ScriptObject _object;

        public ScriptProxy(ScriptObject __object, ScriptObject options)
        {
            if (options.GetPropertyValue("get") is ClosureFunction getFunc)
            {
                _getter = getFunc;
            }

            if (options.GetPropertyValue("set") is ClosureFunction setFunc)
            {
                _setter = setFunc;
            }

            if (options.GetPropertyValue("unset") is ClosureFunction deleteFunc)
            {
                _delete = deleteFunc;
            }

            _object = __object;
        }



        internal sealed override Boolean DeletePropertyValue(ScriptContext ctx, String key)
        {
            if (_delete != null)
            {
                if (ctx != null)
                {
                    ctx = ctx.With(_getter);
                }
                else
                {
                    ctx = new ScriptContext(_getter.Domain);
                }
                _delete.Invoke(ctx, [ScriptDatum.FromObject(_object), ScriptDatum.FromString(key)]);
            }
            return true;
        }



        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, String key)
        {
            if (_getter != null)
            {
                if (ctx != null)
                {
                    ctx = ctx.With(_getter);
                }
                else
                {
                    ctx = new ScriptContext(_getter.Domain);
                }
                var result = _getter.Invoke(ctx, [ScriptDatum.FromObject(_object), ScriptDatum.FromString(key)]);
                return ScriptDatum.ToObject(result);
            }
            return ScriptObject.Null;
        }

        internal sealed override void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            if (_setter != null)
            {
                if (ctx != null)
                {
                    ctx = ctx.With(_getter);
                }
                else
                {
                    ctx = new ScriptContext(_getter.Domain);
                }
                _setter.Invoke(ctx, [ScriptDatum.FromObject(_object), ScriptDatum.FromString(key), ScriptDatum.FromObject(value)]);
            }
        }

        public sealed override void Define(String key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            SetPropertyValue(null, key, value);
        }

    }
}
