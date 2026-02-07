using AuroraScript.Runtime.Types;
using System;
using System.Text;


namespace AuroraScript.Runtime.Extensions
{

    internal class StringBufferConstructor : ScriptType
    {
        public static readonly ScriptObject StringBufferPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        internal static StringBufferConstructor INSTANCE = new StringBufferConstructor();

        static StringBufferConstructor()
        {

            StringBufferPrototype.Define("toString", new BondingFunction(StringBuffer.TO_STRING), writeable: false, enumerable: false);
            StringBufferPrototype.Define("append", new BondingFunction(StringBuffer.APPEND), writeable: false, enumerable: false);
            StringBufferPrototype.Define("insert", new BondingFunction(StringBuffer.INSERT), writeable: false, enumerable: false);
            StringBufferPrototype.Define("appendLine", new BondingFunction(StringBuffer.APPEND_LINE), writeable: false, enumerable: false);
            StringBufferPrototype.Define("clear", new BondingFunction(StringBuffer.CLEAR), writeable: false, enumerable: false);
            StringBufferPrototype.Frozen();
        }

        internal StringBufferConstructor() : base("StringBuffer")
        {
            _prototype = Prototypes.ScriptObjectConstructorPrototype;
        }

        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
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








    internal class StringBuffer : ScriptObject
    {
        private readonly StringBuilder _builder;

        public StringBuffer()
        {
            _prototype = StringBufferConstructor.StringBufferPrototype;
            _builder = new StringBuilder();
        }

        public StringBuffer(String initialValue)
        {
            _prototype = StringBufferConstructor.StringBufferPrototype;
            _builder = new StringBuilder(initialValue);
        }

        public override string ToString()
        {
            return _builder.ToString();
        }


        public int Length => _builder.Length;


        internal static void TO_STRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                ScriptDatum.WriteAsString(ref result, builder._builder.ToString());
            }
        }

        internal static void APPEND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder._builder.Append(ScriptDatum.ToString(args[i]));
                }
            }
        }
        internal static void INSERT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                if (args.TryGetInteger(0, out var index) && args.TryGetString(1, out var str))
                {
                    builder._builder.Insert((int)index, str);
                }
            }
        }
        internal static void APPEND_LINE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder._builder.Append(ScriptDatum.ToString(args[i]));
                }
                builder._builder.AppendLine();
            }
        }
        internal static void CLEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                builder._builder.Clear();
            }
        }

    }
}
