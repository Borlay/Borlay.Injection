using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    public interface ICreateFactory
    {
        ResolverItem<object> Create();
        ResolverItem<T> Create<T>();
    }

    public class ResolverItemFactory<TResult> : ICreateFactory
    {
        public Func<Tuple<TResult, Action>> Factory { get; private set; }

        public bool IsSingletone { get; private set; }

        public ResolverItemFactory(Func<Tuple<TResult, Action>> factory, bool isSingletone)
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
            this.Factory = () => tuple;
            this.IsSingletone = true;
        }

        public ResolverItemFactory(Func<TResult> singletoneProvider)
        {
            if (singletoneProvider == null)
                throw new ArgumentNullException(nameof(singletoneProvider));

            Tuple<TResult, Action> tuple = null;
            this.Factory = () =>
            {
                return tuple ?? (tuple = new Tuple<TResult, Action>(singletoneProvider(), null));
            };
            this.IsSingletone = true;
        }

        public ResolverItem<object> Create()
        {
            var result = Factory.Invoke();
            return new ResolverItem<object>(result.Item1, result.Item2, IsSingletone);
        }

        public ResolverItem<T> Create<T>()
        {
            var result = Factory.Invoke();
            return new ResolverItem<T>((T)(object)result.Item1, result.Item2, IsSingletone);
        }
    }
}
