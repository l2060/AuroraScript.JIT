using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Engine-scoped index of native types that opted into TDoc through
    /// <see cref="INativeTypedDocument"/>. A factory is compiled once so deserialize
    /// and script literals do not use <see cref="Activator"/> on the hot path.
    /// Host types usually omit construction: the NativeType generator emits
    /// <c>CreateTypedDocument()</c> or a parameterless constructor.
    /// </summary>
    internal sealed class TypedDocumentNativeCatalog
    {
        private readonly Dictionary<string, Entry> _byName;
        private readonly Dictionary<Type, Entry> _byType;

        public TypedDocumentNativeCatalog(IReadOnlyList<Type> nativeTypes)
        {
            _byName = new Dictionary<string, Entry>(StringComparer.Ordinal);
            _byType = new Dictionary<Type, Entry>();
            if (nativeTypes == null)
            {
                return;
            }

            for (var i = 0; i < nativeTypes.Count; i++)
            {
                Add(nativeTypes[i]);
            }
        }

        public bool TryGet(string typeName, out Entry entry)
        {
            if (typeName == null)
            {
                entry = null;
                return false;
            }

            return _byName.TryGetValue(typeName, out entry);
        }

        public bool TryGet(Type clrType, out Entry entry)
        {
            if (clrType == null)
            {
                entry = null;
                return false;
            }

            return _byType.TryGetValue(clrType, out entry);
        }

        private void Add(Type nativeType)
        {
            if (nativeType == null ||
                nativeType.Assembly == typeof(AuroraEngine).Assembly ||
                !typeof(INativeTypedDocument).IsAssignableFrom(nativeType) ||
                !typeof(ScriptObject).IsAssignableFrom(nativeType))
            {
                return;
            }

            var attribute = nativeType.GetCustomAttribute<AuroraNativeTypeAttribute>();
            if (attribute == null || string.IsNullOrEmpty(attribute.TypeName))
            {
                return;
            }

            var entry = new Entry(attribute.TypeName, nativeType, CompileFactory(nativeType));
            if (!_byName.TryAdd(entry.TypeName, entry))
            {
                throw new InvalidOperationException(
                    $"Duplicate TDoc native type '{entry.TypeName}'.");
            }

            _byType[nativeType] = entry;
        }

        private static Func<INativeTypedDocument> CompileFactory(Type type)
        {
            var create = type.GetMethod(
                "CreateTypedDocument",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (create != null && typeof(INativeTypedDocument).IsAssignableFrom(create.ReturnType))
            {
                return Expression.Lambda<Func<INativeTypedDocument>>(
                    Expression.Convert(Expression.Call(create), typeof(INativeTypedDocument))).Compile();
            }

            var constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (constructor != null)
            {
                return Expression.Lambda<Func<INativeTypedDocument>>(
                    Expression.Convert(Expression.New(constructor), typeof(INativeTypedDocument))).Compile();
            }

            throw new InvalidOperationException(
                $"Native type '{type.FullName}' implements INativeTypedDocument but cannot be constructed. " +
                "The NativeType generator supplies CreateTypedDocument() when the type has a user constructor; " +
                "otherwise a parameterless constructor is required.");
        }

        internal sealed class Entry
        {
            public Entry(string typeName, Type clrType, Func<INativeTypedDocument> create)
            {
                TypeName = typeName;
                ClrType = clrType;
                Create = create;
            }

            public string TypeName { get; }

            public Type ClrType { get; }

            public Func<INativeTypedDocument> Create { get; }
        }
    }
}
