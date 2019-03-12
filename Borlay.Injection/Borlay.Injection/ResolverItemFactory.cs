using System;
using System.Collections.Generic;
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

        public ResolverItemFactory(Func<IResolverSession, Tuple<TResult, Action>> factory, bool isSingletone)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            this.Factory = factory;
            this.IsSingletone = isSingletone;
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

        public ResolverItem<object> Create(IResolverSession session)
        {
            var result = Factory.Invoke(session);
            return new ResolverItem<object>(result.Item1, result.Item2, IsSingletone);
        }

        public ResolverItem<T> Create<T>(IResolverSession session)
        {
            var result = Factory.Invoke(session);
            return new ResolverItem<T>((T)(object)result.Item1, result.Item2, IsSingletone);
        }
    }
}
