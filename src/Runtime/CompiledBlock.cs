using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Threading;
using System.Collections.Generic;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a compiled lightweight script block.
    /// </summary>
    public sealed class CompiledBlock : IDisposable
    {
        private readonly AuroraEngine _engine;
        private ScriptFunctionDelegate _target;
        private ScriptDomain _boundDomain;
        private ImportBinding[] _imports = Array.Empty<ImportBinding>();
        private int[] _dynamicDelegateIds;
        private int _disposed;

        internal CompiledBlock(AuroraEngine engine, ScriptFunctionDelegate target, int[] dynamicDelegateIds)
        {
            _engine = engine;
            _target = target;
            _dynamicDelegateIds = dynamicDelegateIds ?? Array.Empty<int>();
        }

        internal void BindImports(ScriptDomain domain, ImportBinding[] imports)
        {
            _boundDomain = domain;
            _imports = imports;
        }

        internal readonly struct ImportBinding
        {
            internal readonly ScriptModule Module;
            internal readonly KeyValuePair<string, ScriptDatum>[] StaticMembers;
            internal ImportBinding(ScriptModule module, KeyValuePair<string, ScriptDatum>[] members)
            {
                Module = module;
                StaticMembers = members;
            }
        }

        private void ValidateImports(ScriptDomain domain)
        {
            if (_boundDomain == null) return;
            if (!ReferenceEquals(domain, _boundDomain))
                throw new AuroraException("This compiled block is bound to a different domain.");
            foreach (var import in _imports)
            {
                if (!ReferenceEquals(domain.Global.GetModuleByPath(import.Module.Source.FullPath), import.Module))
                    throw new AuroraException($"Imported module '{import.Module.Source.FullPath}' is no longer loaded. Recompile the block.");
                foreach (var member in import.StaticMembers)
                    if (!import.Module.TryGetExport(member.Key, out var value, out var readOnly) ||
                        (!readOnly && !import.Module.IsNativeFunction(member.Key)) || !value.SameBits(member.Value))
                        throw new AuroraException($"Static import '{member.Key}' changed. Recompile the block.");
            }
        }

        /// <summary>
        /// Releases dynamic delegates registered while compiling this block.
        /// </summary>
        public void Dispose()
        {
            ReleaseDynamicDelegates();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases dynamic delegates if the block is garbage collected without explicit disposal.
        /// </summary>
        ~CompiledBlock()
        {
            ReleaseDynamicDelegates();
        }

        /// <summary>
        /// Invokes the compiled block in the specified domain without arguments.
        /// </summary>
        public ScriptDatum Invoke(ScriptDomain domain)
        {
            return Invoke(domain, Array.Empty<ScriptDatum>());
        }

        /// <summary>
        /// Invokes the compiled block in the specified domain with raw script arguments.
        /// </summary>
        public ScriptDatum Invoke(ScriptDomain domain, params ScriptDatum[] arguments)
        {
            ThrowIfDisposed();
            var ctx = domain.ContextPool.Rent(domain, domain.UserState, null, null);
            try
            {
                return _imports.Length == 0 ? _target(ctx, arguments) : Invoke(ctx, arguments.AsSpan());
            }
            finally
            {
                ctx.Release();
            }
        }

        /// <summary>
        /// Invokes the compiled block in the specified domain with script object arguments.
        /// </summary>
        public ScriptDatum Invoke(ScriptDomain domain, params ScriptObject[] arguments)
        {
            return Invoke(domain, ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// Invokes the compiled block in its bound domain, or a new empty domain when it has no imports.
        /// </summary>
        public ScriptDatum Invoke(params ScriptDatum[] arguments)
        {
            ThrowIfDisposed();
            var domain = _boundDomain ?? _engine.CreateEmptyDomain(null);
            return Invoke(domain, arguments);
        }

        /// <summary>
        /// Invokes the compiled block in its bound domain, or a new empty domain when it has no imports.
        /// </summary>
        public ScriptDatum Invoke(params ScriptObject[] arguments)
        {
            return Invoke(ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// Invokes the compiled block using an existing script context.
        /// </summary>
        public ScriptDatum Invoke(ScriptContext context, ReadOnlySpan<ScriptDatum> arguments)
        {
            ThrowIfDisposed();
            ValidateImports(context.Domain);
            if (_imports.Length == 0) return InvokeCore(context, arguments);
            var count = _imports.Length + arguments.Length;
            if (count <= 8)
            {
                DatumBuffer8 buffer = default;
                return InvokeWithImports(context, arguments, ((Span<ScriptDatum>)buffer)[..count]);
            }
            var rented = CallOps.RentArguments(count);
            try { return InvokeWithImports(context, arguments, rented.AsSpan(0, count)); }
            finally { CallOps.ReturnArguments(rented, count); }
        }

        private ScriptDatum InvokeWithImports(ScriptContext context, ReadOnlySpan<ScriptDatum> arguments, Span<ScriptDatum> buffer)
        {
            for (var i = 0; i < _imports.Length; i++)
                buffer[i] = ScriptDatum.FromObject(_imports[i].Module);
            arguments.CopyTo(buffer[_imports.Length..]);
            var frame = context.EnterModule(null);
            try { return _target(context, buffer); }
            finally { context.LeaveFrame(frame); }
        }

        private ScriptDatum InvokeCore(ScriptContext context, ReadOnlySpan<ScriptDatum> arguments)
        {
            switch (arguments.Length)
            {
                case 0:
                    return _target(context, Span<ScriptDatum>.Empty);
                case 1:
                    {
                        DatumBuffer1 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 2:
                    {
                        DatumBuffer2 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 3:
                    {
                        DatumBuffer3 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 4:
                    {
                        DatumBuffer4 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 5:
                    {
                        DatumBuffer5 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 6:
                    {
                        DatumBuffer6 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 7:
                    {
                        DatumBuffer7 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                case 8:
                    {
                        DatumBuffer8 buffer = default;
                        CopyArguments(arguments, buffer);
                        return _target(context, buffer);
                    }
                default:
                    var rented = CallOps.RentArguments(arguments.Length);
                    try
                    {
                        arguments.CopyTo(rented);
                        return _target(context, rented.AsSpan(0, arguments.Length));
                    }
                    finally
                    {
                        CallOps.ReturnArguments(rented, arguments.Length);
                    }
            }
        }

        private static void CopyArguments(ReadOnlySpan<ScriptDatum> source, Span<ScriptDatum> target)
        {
            source.CopyTo(target);
        }

        internal int[] GetDynamicDelegateIdsSnapshot()
        {
            var ids = _dynamicDelegateIds;
            if (ids.Length == 0)
            {
                return Array.Empty<int>();
            }

            var copy = new int[ids.Length];
            Array.Copy(ids, copy, ids.Length);
            return copy;
        }

        private void ReleaseDynamicDelegates()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            var ids = Interlocked.Exchange(ref _dynamicDelegateIds, Array.Empty<int>());
            for (var i = 0; i < ids.Length; i++)
            {
                DynamicMethodRegistry.Unregister(ids[i]);
            }
            _target = null;
            _boundDomain = null;
            _imports = Array.Empty<ImportBinding>();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(CompiledBlock));
            }
        }
    }
}
