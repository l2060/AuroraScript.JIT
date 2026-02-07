using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// Describes a .NET type that has been exposed to AuroraScript and maintains a cache of its member metadata.
    /// This class uses Expression Trees to compile optimized accessors for properties, fields, and methods.
    /// </summary>
    internal sealed class ClrTypeDescriptor
    {
        private readonly ConcurrentDictionary<string, ClrMethodBinding> _methodCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ClrGetterBinding> _getterCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, ClrSetterBinding> _setterCache = new(StringComparer.Ordinal);
        private readonly BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        public readonly Type Type;


        internal ClrTypeDescriptor(Type type)
        {
            Type = type;
        }


        /// <summary>
        /// Retrieves or resolves a bound method (overload group) from the cache.
        /// </summary>
        /// <param name="name">The name of the method to retrieve.</param>
        /// <param name="isStatic">True to look for static methods; false for instance methods.</param>
        /// <returns>A <see cref="ClrMethodBinding"/> representing the method(s), or null if not found.</returns>
        public ClrMethodBinding GetMethods(string name, Boolean isStatic)
        {
            return _methodCache.GetOrAdd(name, e => ResolveMethods(name, isStatic));
        }

        /// <summary>
        /// Retrieves or compiles a getter delegate for the specified property or field.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        /// <returns>A <see cref="ClrGetterBinding"/> delegate, or null if not found.</returns>
        public ClrGetterBinding GetGetter(string name)
        {
            return _getterCache.GetOrAdd(name, CompileGetter);
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
            return _setterCache.GetOrAdd(name, CompileSetter);
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
    }
}

