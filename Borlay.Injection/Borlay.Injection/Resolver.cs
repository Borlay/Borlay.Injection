using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Borlay.Injection
{
    public class Resolver : IResolver
    {
        private readonly ConcurrentDictionary<Type, ICreateFactory> providers = new ConcurrentDictionary<Type, ICreateFactory>();
        private readonly ConcurrentDictionary<Type, object> instances = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentStack<IDisposable> disposables = new ConcurrentStack<IDisposable>();

        public IResolver Parent { get; }

        public bool AddFromParent { get; set; } = false;

        private volatile bool isDisposed = false;

        public Resolver()
            : this(null)
        {
        }

        public Resolver(IResolver parent)
        {
            this.Parent = parent;
            Register(this, true);
        }

        // todo add Contains

        public bool Contains<T>(bool parent)
        {
            return Contains(typeof(T), parent);
        }

        public bool Contains(Type type, bool parent)
        {
            if (providers.ContainsKey(type)) return true;
            if (parent && Parent != null) return Parent.Contains(type, parent);
            return false;
        }

        public void Register<T>(Func<IResolverSession, Tuple<T, Action>> provider, bool isSingletone, bool includeBase = true)
        {
            Register(typeof(T), new ResolverItemFactory<T>(provider, isSingletone), includeBase);
        }

        public void Register(Type type, ICreateFactory itemFactory, bool includeBase = true)
        {
            providers[type] = itemFactory;

            if (includeBase)
            {
                var baseType = type.GetTypeInfo().BaseType;
                if(baseType != null && baseType != typeof(object))
                    Register(baseType, itemFactory, includeBase);

                foreach(var interf in type.GetTypeInfo().GetInterfaces())
                {
                    Register(interf, itemFactory, includeBase);
                }
            }
        }

        public void RegisterAs<T>(T instance, bool includeBase = true)
        {
            Register(typeof(T), new ResolverItemFactory<T>(instance), includeBase);
        }

        public void Register(object instance, bool includeBase = true)
        {
            Register(instance.GetType(), new ResolverItemFactory<object>(instance), includeBase);
        }

        public void Register<T>(bool includeBase, bool singleton = false) where T : class, new()
        {
            Register(typeof(T), includeBase, singleton);
        }

        public void Register(Type type, bool includeBase, bool singleton = false)
        {
            if(singleton)
                Register(type, new ResolverItemFactory<object>((s) => GetSingletoneInstance(type)), includeBase);
            else
            {
                Register(type, new ResolverItemFactory<object>(type.GetTypeInfo()), includeBase);
                //(session) =>
                //{
                //    var instance = session.CreateInstance(type);
                //    return new Tuple<object, Action>(instance, null);
                //}, false), includeBase);
            }
        }

        //protected virtual object GetInstance<T>(bool singletone)
        //{
        //    return GetInstance(typeof(T), singletone);
        //}

        //protected virtual object GetInstance(Type type, bool singletone)
        //{
        //    if(singletone)
        //        return GetSingletoneInstance(type);

        //    return CreateInstance(type);
        //}

        public virtual T GetSingletoneInstance<T>()
        {
            return (T)GetSingletoneInstance(typeof(T));
        }

        public virtual object GetSingletoneInstance(Type type)
        {
            if (instances.TryGetValue(type, out var value))
                return value;
            else if(providers.ContainsKey(type))
            {
                var instance = CreateSingletoneInstance(type);
                instances[type] = instance;
                return instance;
            }
            else if (Parent != null && Parent is Resolver)
                return ((Resolver)Parent).GetSingletoneInstance(type);

            return CreateSingletoneInstance(type);
        }

        protected object CreateSingletoneInstance(Type type)
        {
            var session = this.CreateSession();
            try
            {
                var instance = session.CreateInstance(type);
                disposables.Push(session);
                return instance;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public void AddDisposable(IDisposable disposable)
        {
            disposables.Push(disposable);
        }

        //public virtual T CreateInstance<T>()
        //{
        //    return (T)CreateInstance(typeof(T));
        //}

        //public virtual object CreateInstance(Type type)
        //{
        //    return CreateInstance(type.GetTypeInfo());
        //}

        //public virtual object CreateInstance(TypeInfo typeInfo)
        //{
        //    return CreateInstance(this, typeInfo);
        //}

        public ICreateFactory Resolve<T>()
        {
            return Resolve(typeof(T));
        }

        public ICreateFactory Resolve(Type type)
        {
            if (TryResolve(type, out var value))
                return value;

            throw new KeyNotFoundException($"Instance for type '{type.Name}' not found");
        }

        public bool TryResolve<T>(out ICreateFactory createFactory)
        {
            createFactory = null;
            if(TryResolve(typeof(T), out createFactory))
            {
                return true;
            }
            return false;
        }

        public bool TryResolve(Type type, out ICreateFactory createFactory)
        {
            createFactory = null;
            if (providers.TryGetValue(type, out createFactory))
                return true;

            var resolved = Parent?.TryResolve(type, out createFactory) ?? false;

            if (resolved && AddFromParent)
                Register(type, createFactory, false);

            return resolved;
        }

        public virtual void LoadFromReference<T>()
        {
            LoadFromReference(typeof(T));
        }

        public virtual void LoadFromReference(Type referenceType)
        {
            var types = GetTypesFromReference<ResolveAttribute>(referenceType);
            foreach (var type in types)
            {
                var resolve = GetCustomAttribute<ResolveAttribute>(type.GetTypeInfo());
                var singletone = resolve.Singletone;
                Register(resolve.AsType ?? type, resolve.IncludeBase, resolve.Singletone);
            }
        }

        public static IEnumerable<Type> GetTypesFromReference<TAttribute>(Type referenceType) where TAttribute : Attribute
        {
            var assembly = referenceType.GetTypeInfo().Assembly;
            var assemblyNames = assembly.GetReferencedAssemblies();

            var assemblies = assemblyNames.Select(a => Assembly.Load(a)).ToList();
            assemblies.Add(assembly);

            return from a in assemblies
                   from t in a.GetTypes()
                   let i = t.GetTypeInfo()
                   where GetCustomAttribute<TAttribute>(i) != null
                   && i.IsClass && !i.IsAbstract
                   select t;
        }

        public static T GetCustomAttribute<T>(TypeInfo typeInfo)
        {
            return GetCustomAttributes<T>(typeInfo).FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(TypeInfo typeInfo)
        {
            var attributeType = typeof(T);
            return typeInfo.GetCustomAttributes(attributeType, true).
              Union(typeInfo.GetInterfaces().
              SelectMany(interfaceType => interfaceType.GetTypeInfo().GetCustomAttributes(attributeType, true))).
              Distinct().Cast<T>();
        }

        public IResolverSession CreateSession()
        {
            return new ResolverSession(this);
        }

        public void Dispose()
        {
            if (!TryDispose(out var exception))
                throw exception;
        }

        public bool TryDispose(out AggregateException aggregateException)
        {
            isDisposed = true;

            var list = instances.Select(i => i.Value).OfType<IDisposable>().ToList();
            list.AddRange(disposables);

            return list.TryDispose(out aggregateException);
        }
    }
}
