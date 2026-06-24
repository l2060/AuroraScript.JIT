using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Threading;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a compiled lightweight script block.
    /// </summary>
    public sealed class CompiledBlock : IDisposable
    {
        private readonly AuroraEngine _engine;
        private readonly ScriptFunctionDelegate _target;
        private int[] _dynamicDelegateIds;
        private int _disposed;

        internal CompiledBlock(AuroraEngine engine, ScriptFunctionDelegate target, int[] dynamicDelegateIds)
        {
            _engine = engine;
            _target = target;
            _dynamicDelegateIds = dynamicDelegateIds ?? Array.Empty<int>();
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
                return _target(ctx, arguments);
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
        /// Invokes the compiled block in a new empty domain with raw script arguments.
        /// </summary>
        public ScriptDatum Invoke(params ScriptDatum[] arguments)
        {
            ThrowIfDisposed();
            var domain = _engine.CreateEmptyDomain(null);
            return Invoke(domain, arguments);
        }

        /// <summary>
        /// Invokes the compiled block in a new empty domain with script object arguments.
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
                    var rented = CILHelper.RentArguments(arguments.Length);
                    try
                    {
                        arguments.CopyTo(rented);
                        return _target(context, rented.AsSpan(0, arguments.Length));
                    }
                    finally
                    {
                        CILHelper.ReturnArguments(rented, arguments.Length);
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
