using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Borlay.Injection
{
    public static class Extensions
    {
        public static bool TryDispose(this IEnumerable<IDisposable> disposables, out AggregateException aggregateException)
        {
            List<Exception> exceptions = new List<Exception>();
            if (disposables.Count() > 0)
            {
                IDisposable[] array = disposables.ToArray();
                for (int i = 0; i < array.Length; i++)
                {
                    try
                    {
                        array[i].Dispose();
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
            }

            aggregateException = null;
            if (exceptions.Count > 0)
            {
                aggregateException = new AggregateException(exceptions);
                return false;
            }
            return true;
        }

        public static Tuple<object, Action> ToObject<T>(this Func<Tuple<T, Action>> provider)
        {
            var result = provider.Invoke();
            return new Tuple<object, Action>(result.Item1, result.Item2);
        }

        public static T CreateInstance<T>(this IResolverSession resolver)
        {
            return (T)CreateInstance(resolver, typeof(T));
        }

        public static object CreateInstance(this IResolverSession resolver, Type type)
        {
            return CreateInstance(resolver, type.GetTypeInfo());
        }

        public static bool TryCreateInstance(this IResolverSession resolver, Type type, out object obj)
        {
            return TryCreateInstance(resolver, type.GetTypeInfo(), out obj);
        }

        public static object CreateInstance(this IResolverSession resolver, TypeInfo typeInfo)
        {
            if (TryCreateInstance(resolver, typeInfo, out var obj))
                return obj;

            throw new KeyNotFoundException($"Constructor for type $'{typeInfo.Name}' not found");
        }

        public static bool TryCreateInstance(this IResolverSession resolver, TypeInfo typeInfo, out object obj)
        {
            var constructors = typeInfo.GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToArray();

            foreach (var constructorInfo in constructors)
            {
                if (TryCreateInstance(resolver, constructorInfo, out obj))
                    return true;
            }

            obj = null;
            return false;
        }

        public static object CreateInstance(this IResolverSession resolver, ConstructorInfo constructorInfo)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            if (constructorInfo == null)
                throw new ArgumentNullException(nameof(constructorInfo));

            var parameters = constructorInfo.GetParameters();
            var arguments = parameters.Select(p => resolver.Resolve(p.ParameterType)).ToArray();
            return constructorInfo.Invoke(arguments);
        }

        public static bool TryCreateInstance(this IResolverSession resolver, ConstructorInfo constructorInfo, out object obj)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            if (constructorInfo == null)
                throw new ArgumentNullException(nameof(constructorInfo));

            obj = null;

            var parameters = constructorInfo.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!resolver.TryResolve(parameters[i].ParameterType, out var value))
                    return false;

                arguments[i] = value;
            }
            obj = constructorInfo.Invoke(arguments);
            return true;
        }
    }
}
