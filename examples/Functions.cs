using AuroraScript;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Security.Cryptography;
using System.Text;
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
                // ScriptContext belongs to the current synchronous call and is pooled as
                // soon as this method returns. Only retain the stable user state.
                var userState = ctx.UserState;
                Task.Run(async () =>
                {
                    // 模拟回调调用
                    await Task.Delay(1000);
                    callback.InvokeClrDetached(userState, 123);
                });
            }
        }

        public static void MD5_NATIVE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var input = args.Length > 0 ? ScriptDatum.ToString(args[0]) : string.Empty;
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            Span<char> hex = stackalloc char[hash.Length];


            int idx = 0;
            foreach (var b in hash)
            {
                hex[idx++] = b.ToString("x2")[0];// GetHex((byte)(b >> 4));
            }
            result = ScriptDatum.FromString(new string(hex));
        }

    }
}
