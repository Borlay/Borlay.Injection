using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    public class ResolverSession : IResolverSession
    {
        private readonly IResolver resolver;
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public IResolver Resolver => resolver;

        public ResolverSession(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public bool Contains<T>(bool parent)
        {
            return resolver.Contains<T>(parent);
        }

        public bool Contains(Type type, bool parent)
        {
            return resolver.Contains(type, parent);
        }

        public void Dispose()
        {
            while(disposables.Count > 0)
            {
                disposables[0].Dispose();
                disposables.RemoveAt(0);
            }
        }

        public T Resolve<T>()
        {
            var item = resolver.Resolve<T>();
            if (!item.IsSingletone)
                disposables.Add(item);

            return item.Result;
        }

        public object Resolve(Type type)
        {
            var item = resolver.Resolve(type);
            if (!item.IsSingletone)
                disposables.Add(item);

            return item.Result;
        }

        public bool TryResolve<T>(out T value)
        {
            value = default(T);
            if(resolver.TryResolve<T>(out var item))
            {
                if (!item.IsSingletone)
                    disposables.Add(item);

                value = item.Result;
                return true;
            }

            return false;
        }

        public bool TryResolve(Type type, out object value)
        {
            value = null;
            if (resolver.TryResolve(type, out var item))
            {
                if (!item.IsSingletone)
                    disposables.Add(item);

                value = item.Result;
                return true;
            }

            return false;
        }
    }
}
