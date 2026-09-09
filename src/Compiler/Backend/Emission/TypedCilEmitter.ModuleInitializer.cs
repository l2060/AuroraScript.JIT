using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        internal void EmitInitializerBody(ILGenerator il, Action emitBody)
        {
            _function = _module.InitializerFunction;
            _code = _moduleCode.Initializer;
            _il = il;
            _convention = FunctionCallConvention.Span;
            _locals = Array.Empty<LocalBuilder>();
            _numericCacheNeeded = Array.Empty<bool>();
            _booleanCacheNeeded = Array.Empty<bool>();
            _hasArgumentBufferCleanup = PooledArgumentCallDetector.Contains(
                _function.Declaration, CallUsesArgumentBuffer, ConstructorUsesArgumentBuffer);
            _argumentBuffers = _hasArgumentBufferCleanup
                ? new List<(LocalBuilder Arguments, LocalBuilder Count)>() : null;
            if (_hasArgumentBufferCleanup) _il.BeginExceptionBlock();
            try
            {
                emitBody();
                if (_hasArgumentBufferCleanup)
                {
                    _il.BeginFinallyBlock();
                    foreach (var buffer in _argumentBuffers)
                    {
                        _il.Emit(OpCodes.Ldloc, buffer.Arguments);
                        _il.Emit(OpCodes.Ldloc, buffer.Count);
                        _il.Emit(OpCodes.Call, TypedRuntimeMetadata.ReturnArguments);
                    }
                    _il.EndExceptionBlock();
                }
            }
            finally
            {
                _function = null;
                _code = null;
                _il = null;
                _locals = null;
                _argumentBuffers = null;
                _hasArgumentBufferCleanup = false;
            }
        }

        internal void EmitInitializerDatum(Expression expression) => EmitDatum(expression);
        internal void EmitInitializerDiscarded(Expression expression) => EmitExpressionDiscarded(expression);
    }
}
