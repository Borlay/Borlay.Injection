using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Borlay.Injection
{
    public interface ICreateFactory
    {
        ResolverItem<object> Create(IResolverSession session);
        ResolverItem<T> Create<T>(IResolverSession session);
    }

    public class ResolverItemFactory<TResult> : ICreateFactory
    {
        public Func<IResolverSession, Tuple<TResult, Action>> Factory { get; private set; }

        public bool IsSingletone { get; private set; }

        private readonly TypeInfo typeInfo;

        public ResolverItemFactory(Func<IResolverSession, Tuple<TResult, Action>> factory, bool isSingletone)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            this.Factory = factory;
            this.IsSingletone = isSingletone;
        }

        public ResolverItemFactory(TypeInfo typeInfo)
        {
            this.IsSingletone = false;
            this.typeInfo = typeInfo;
        }

        public ResolverItemFactory(TResult instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var tuple = new Tuple<TResult, Action>(instance, null);
            this.Factory = (s) => tuple;
            this.IsSingletone = true;
        }

        public ResolverItemFactory(Func<IResolverSession, TResult> singletoneProvider)
        {
            if (singletoneProvider == null)
                throw new ArgumentNullException(nameof(singletoneProvider));

            Tuple<TResult, Action> tuple = null;
            this.Factory = (s) =>
            {
                return tuple ?? (tuple = new Tuple<TResult, Action>(singletoneProvider(s), null));
            };
            this.IsSingletone = true;
        }

        protected virtual Func<IResolverSession, Tuple<TResult, Action>> GetFactory(IResolverSession session)
        {
            lock (this)
            {
                if (Factory != null) return Factory;

                var constructors = typeInfo.GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToArray();

                foreach (var constructorInfo in constructors)
                {
                    if (session.TryCreateInstance(constructorInfo, out var obj))
                    {
                        var _cinfo = constructorInfo;
                        Factory = (s) =>
                        {
                            s.TryCreateInstance(_cinfo, out var instance);
                            return new Tuple<TResult, Action>((TResult)instance, null);
                        };

                        return (s) => new Tuple<TResult, Action>((TResult)obj, null);
                    }
                }

                Factory = (s) =>
                {
                    var instance = session.CreateInstance(typeInfo);
                    return new Tuple<TResult, Action>((TResult)instance, null);
                };
                return Factory;
            }
        }

        public ResolverItem<object> Create(IResolverSession session)
        {
            var result = GetFactory(session).Invoke(session);
            return new ResolverItem<object>(result.Item1, result.Item2, IsSingletone);
        }

        public ResolverItem<T> Create<T>(IResolverSession session)
        {
            var result = GetFactory(session).Invoke(session);
            return new ResolverItem<T>((T)(object)result.Item1, result.Item2, IsSingletone);
        }
    }
}
