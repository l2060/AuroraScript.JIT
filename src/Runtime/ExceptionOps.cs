using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    internal abstract class ScriptControlSignal : Exception
    {
    }

    internal sealed class ScriptReturnSignal : ScriptControlSignal
    {
        public ScriptReturnSignal(ScriptDatum value)
        {
            Value = value;
        }

        public ScriptDatum Value { get; }
    }

    internal sealed class ScriptLoopTransferSignal : ScriptControlSignal
    {
        public ScriptLoopTransferSignal(bool isContinue)
        {
            IsContinue = isContinue;
        }

        public bool IsContinue { get; }
    }

    internal static class ExceptionOps
    {
        public static void Throw(ScriptDatum value)
        {
            if (ScriptDatum.TryGetError(in value, out var error))
            {
                throw new AuroraRuntimeException(error);
            }
            if (value.Kind == ValueKind.Object &&
                value.Object is ClrInstanceObject { Instance: Exception exception })
            {
                throw exception;
            }
            throw new AuroraRuntimeException(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ToScriptError(Exception exception, ScriptContext context)
        {
            if (exception is AuroraRuntimeException runtime && runtime.internalError != null)
            {
                context?.ClearExceptionStack();
                return ScriptDatum.FromError(runtime.internalError);
            }

            var trace = context?.TakeExceptionStack() ?? [];
            return ScriptDatum.FromError(new ScriptError(exception.Message, trace));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReturnFromFinally(ScriptDatum value)
        {
            throw new ScriptReturnSignal(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BreakFromFinally()
        {
            throw new ScriptLoopTransferSignal(isContinue: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ContinueFromFinally()
        {
            throw new ScriptLoopTransferSignal(isContinue: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception PrepareCatch(Exception exception)
        {
            if (exception is ScriptControlSignal) throw exception;
            return exception;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetReturnValue(ScriptReturnSignal signal)
        {
            return signal.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsContinue(ScriptLoopTransferSignal signal)
        {
            return signal.IsContinue;
        }
    }
}
