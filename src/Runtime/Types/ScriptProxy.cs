using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a proxy object in AuroraScript that intercepts operations on a target object.
    /// This allows for custom behavior when getting, setting, or deleting properties.
    /// </summary>
    public class ScriptProxy : ScriptObject
    {
        private readonly ClosureFunction _getter;
        private readonly ClosureFunction _setter;
        private readonly ClosureFunction _delete;
        private readonly ScriptObject _object;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptProxy"/> class.
        /// </summary>
        /// <param name="__object">The target object to be proxied.</param>
        /// <param name="options">An object containing "get", "set", or "unset" handler functions.</param>
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



        /// <inheritdoc />
        protected internal sealed override Boolean DeletePropertyValue(ScriptContext ctx, String key)
        {
            if (_delete != null)
            {
                InvokeHandler(ctx, _delete, ScriptDatum.FromObject(_object), ScriptDatum.FromString(key));
            }
            return true;
        }



        internal sealed override ScriptObject GetPropertyValue(ScriptContext ctx, String key)
        {
            return ScriptDatum.ToObject(GetPropertyDatum(ctx, key));
        }

        /// <inheritdoc />
        protected internal sealed override ScriptDatum GetPropertyDatum(ScriptContext ctx, String key)
        {
            if (_getter != null)
            {
                return InvokeHandler(ctx, _getter, ScriptDatum.FromObject(_object), ScriptDatum.FromString(key));
            }
            return ScriptDatum.Null;
        }

        internal sealed override void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            SetPropertyDatum(ctx, key, ScriptDatum.FromObject(value));
        }

        /// <inheritdoc />
        protected internal sealed override void SetPropertyDatum(ScriptContext ctx, string key, ScriptDatum value)
        {
            if (_setter != null)
            {
                InvokeHandler(ctx, _setter, ScriptDatum.FromObject(_object), ScriptDatum.FromString(key), value);
            }
        }

        private static ScriptDatum InvokeHandler(ScriptContext ctx, ClosureFunction handler, ScriptDatum target, ScriptDatum key)
        {
            if (ctx != null)
            {
                return handler.Invoke(ctx, target, key);
            }

            var root = handler.Domain.ContextPool.Rent(handler.Domain, handler.Domain.UserState, null, null);
            try
            {
                return handler.Invoke(root, target, key);
            }
            finally
            {
                ReleaseRoot(root);
            }
        }

        private static ScriptDatum InvokeHandler(ScriptContext ctx, ClosureFunction handler, ScriptDatum target, ScriptDatum key, ScriptDatum value)
        {
            if (ctx != null)
            {
                return handler.Invoke(ctx, target, key, value);
            }

            var root = handler.Domain.ContextPool.Rent(handler.Domain, handler.Domain.UserState, null, null);
            try
            {
                return handler.Invoke(root, target, key, value);
            }
            finally
            {
                ReleaseRoot(root);
            }
        }

        private static void ReleaseRoot(ScriptContext root)
        {
            while (root.Next != null)
            {
                root.Next.ReleaseLinked();
            }
            root.Release();
        }

        /// <summary>
        /// Defines or modifies a property on the proxied object.
        /// This operation is intercepted by the proxy's setter handler if defined.
        /// </summary>
        /// <param name="key">The name of the property to define.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <param name="writeable">Whether the property is writeable.</param>
        /// <param name="enumerable">Whether the property is enumerable.</param>
        public sealed override void Define(String key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            SetPropertyValue(null, key, value);
        }

        /// <summary>
        /// Defines or modifies a datum property on the proxied object.
        /// This operation is intercepted by the proxy's setter handler if defined.
        /// </summary>
        /// <param name="key">The name of the property to define.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <param name="writeable">Whether the property is writeable.</param>
        /// <param name="enumerable">Whether the property is enumerable.</param>
        public sealed override void Define(String key, ScriptDatum value, bool writeable = true, bool enumerable = true)
        {
            SetPropertyDatum(null, key, value);
        }

    }
}
