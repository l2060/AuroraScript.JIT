using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace AuroraScript.Runtime.Interop
{
    internal readonly struct ClrDataMember
    {
        internal ClrDataMember(
            string name,
            Type type,
            int index,
            ClrGetterBinding getter,
            ClrSetterBinding setter)
        {
            Name = name;
            Type = type;
            Index = index;
            Getter = getter;
            Setter = setter;
        }

        internal string Name { get; }
        internal Type Type { get; }
        internal int Index { get; }
        internal ClrGetterBinding Getter { get; }
        internal ClrSetterBinding Setter { get; }
    }

    internal sealed class ClrDataContract
    {
        private readonly Dictionary<string, int> _memberIndexes;

        internal ClrDataContract(ClrDataMember[] members, Func<object> factory)
        {
            Members = members;
            Factory = factory;
            _memberIndexes = new Dictionary<string, int>(members.Length, StringComparer.Ordinal);
            foreach (var member in members) _memberIndexes[member.Name] = member.Index;
        }

        internal ClrDataMember[] Members { get; }
        internal Func<object> Factory { get; }

        internal bool TryGetMember(string name, out ClrDataMember member)
        {
            if (_memberIndexes.TryGetValue(name, out var index))
            {
                member = Members[index];
                return true;
            }
            member = default;
            return false;
        }
    }

    /// <summary>
    /// Describes a .NET type that has been exposed to AuroraScript and maintains a cache of its member metadata.
    /// This class uses Expression Trees to compile optimized accessors for properties, fields, and methods.
    /// </summary>
    internal sealed class ClrTypeDescriptor
    {
        private readonly ConcurrentDictionary<string, ClrMethodBinding> _methodCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ClrGetterBinding> _getterCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ClrSetterBinding> _setterCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _missingGetterCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _missingSetterCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _missingMethodCache = new(StringComparer.Ordinal);
        private readonly BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private ClrDataContract _dataContract;
        public readonly Type Type;


        internal ClrTypeDescriptor(Type type)
        {
            Type = type;
        }

        internal ClrDataContract DataContract
        {
            get
            {
                var contract = Volatile.Read(ref _dataContract);
                if (contract != null) return contract;
                var created = CreateDataContract();
                return Interlocked.CompareExchange(ref _dataContract, created, null) ?? created;
            }
        }


        /// <summary>
        /// Retrieves or resolves a bound method (overload group) from the cache.
        /// </summary>
        /// <param name="name">The name of the method to retrieve.</param>
        /// <param name="isStatic">True to look for static methods; false for instance methods.</param>
        /// <returns>A <see cref="ClrMethodBinding"/> representing the method(s), or null if not found.</returns>
        public ClrMethodBinding GetMethods(string name, Boolean isStatic)
        {
            if (_methodCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (_missingMethodCache.ContainsKey(name))
            {
                return null;
            }

            var resolved = ResolveMethods(name, isStatic);
            if (resolved == null)
            {
                _missingMethodCache.TryAdd(name, 0);
                return null;
            }

            return _methodCache.GetOrAdd(name, resolved);
        }

        /// <summary>
        /// Retrieves or compiles a getter delegate for the specified property or field.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        /// <returns>A <see cref="ClrGetterBinding"/> delegate, or null if not found.</returns>
        public ClrGetterBinding GetGetter(string name)
        {
            if (_getterCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (_missingGetterCache.ContainsKey(name))
            {
                return null;
            }

            var resolved = CompileGetter(name);
            if (resolved == null)
            {
                _missingGetterCache.TryAdd(name, 0);
                return null;
            }

            return _getterCache.GetOrAdd(name, resolved);
        }

        private ClrGetterBinding CompileGetter(string name)
        {
            var prop = Type.GetProperty(name, _bindingFlags);
            if (prop != null && prop.CanRead)
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                Expression body;
                if (prop.GetMethod.IsStatic)
                {
                    body = Expression.Property(null, prop);
                }
                else
                {
                    body = Expression.Property(Expression.Convert(instanceParam, Type), prop);
                }
                return Expression.Lambda<ClrGetterBinding>(Expression.Convert(body, typeof(object)), instanceParam).Compile();
            }

            var field = Type.GetField(name, _bindingFlags);
            if (field != null)
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                Expression body;
                if (field.IsStatic)
                {
                    body = Expression.Field(null, field);
                }
                else
                {
                    body = Expression.Field(Expression.Convert(instanceParam, Type), field);
                }
                return Expression.Lambda<ClrGetterBinding>(Expression.Convert(body, typeof(object)), instanceParam).Compile();
            }

            return null;
        }

        /// <summary>
        /// Retrieves or compiles a setter binding for the specified property or field.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        /// <returns>A <see cref="ClrSetterBinding"/> instance, or null if not found or read-only.</returns>
        public ClrSetterBinding GetSetter(string name)
        {
            if (_setterCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (_missingSetterCache.ContainsKey(name))
            {
                return null;
            }

            var resolved = CompileSetter(name);
            if (resolved == null)
            {
                _missingSetterCache.TryAdd(name, 0);
                return null;
            }

            return _setterCache.GetOrAdd(name, resolved);
        }

        private ClrSetterBinding CompileSetter(string name)
        {
            var prop = Type.GetProperty(name, _bindingFlags);
            if (prop != null && prop.CanWrite)
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var valueParam = Expression.Parameter(typeof(object), "value");
                Expression left;
                if (prop.SetMethod.IsStatic)
                {
                    left = Expression.Property(null, prop);
                }
                else
                {
                    left = Expression.Property(Expression.Convert(instanceParam, Type), prop);
                }
                var assign = Expression.Assign(left, Expression.Convert(valueParam, prop.PropertyType));
                return new ClrSetterBinding(Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile(), prop.PropertyType);
            }

            var field = Type.GetField(name, _bindingFlags);
            if (field != null && !field.IsInitOnly)
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var valueParam = Expression.Parameter(typeof(object), "value");
                Expression left;
                if (field.IsStatic)
                {
                    left = Expression.Field(null, field);
                }
                else
                {
                    left = Expression.Field(Expression.Convert(instanceParam, Type), field);
                }
                var assign = Expression.Assign(left, Expression.Convert(valueParam, field.FieldType));
                return new ClrSetterBinding(Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile(), field.FieldType);
            }

            return null;
        }

        private ClrMethodBinding ResolveMethods(string name, Boolean isStatic)
        {
            BindingFlags resolveFlags = BindingFlags.Public;
            if (isStatic)
            {
                resolveFlags |= BindingFlags.Static;
            }
            else
            {
                resolveFlags |= BindingFlags.Instance;
            }
            var methods = Type.GetMember(name, MemberTypes.Method, resolveFlags);
            if (methods != null)
            {
                var staticMethodBases = methods.OfType<MethodBase>().ToArray();
                if (staticMethodBases.Length > 0)
                {
                    return new ClrMethodBinding(this, staticMethodBases, isStatic);
                }
            }
            return null;
        }

        private ClrDataContract CreateDataContract()
        {
            var members = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var property in Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    property.GetMethod == null ||
                    property.SetMethod == null ||
                    !property.GetMethod.IsPublic ||
                    !property.SetMethod.IsPublic ||
                    property.GetMethod.IsStatic ||
                    property.SetMethod.IsStatic)
                {
                    continue;
                }
                members[property.Name] = property.PropertyType;
            }
            foreach (var field in Type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsStatic || field.IsInitOnly || field.IsLiteral) continue;
                members.TryAdd(field.Name, field.FieldType);
            }

            var result = new ClrDataMember[members.Count];
            var memberIndex = 0;
            foreach (var member in members)
            {
                result[memberIndex++] = new ClrDataMember(member.Key, member.Value, 0, null, null);
            }
            Array.Sort(result, static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            for (var index = 0; index < result.Length; index++)
            {
                var member = result[index];
                result[index] = new ClrDataMember(
                    member.Name,
                    member.Type,
                    index,
                    GetGetter(member.Name),
                    GetSetter(member.Name));
            }

            var constructor = Type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);
            Func<object> factory = null;
            if (constructor != null)
            {
                var body = Expression.Convert(Expression.New(constructor), typeof(object));
                factory = Expression.Lambda<Func<object>>(body).Compile();
            }
            return new ClrDataContract(result, factory);
        }
    }
}

