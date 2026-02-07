using AuroraScript;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Threading.Tasks;

namespace Examples
{
    internal class Functions
    {

        public static void CREATE_TIMER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            //Console.WriteLine(context.UserState);
            if (args.TryGetFunction(0, out var callback))
            {
                callback.InvokeClr(ctx, 123, "timer", ClrMarshaller.ToDatum(Array.Empty<String>()));
            }
        }

        public static void GIVE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            //Console.WriteLine(context.UserState);
            //Console.WriteLine($"GIVE {String.Join(" ", args)}");
        }

        public static void CLIENT_INPUT_NUMBER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            //Console.WriteLine(context.UserState);
            //Console.WriteLine($"OPEN INPUT {String.Join(" ", args)}");
            if (args.TryGetFunction(3, out var callback))
            {
                Task.Run(async () =>
                {
                    // 模拟回调调用
                    await Task.Delay(1000);
                    callback.InvokeClr(ctx, 123);
                });
            }
        }


    }
}
