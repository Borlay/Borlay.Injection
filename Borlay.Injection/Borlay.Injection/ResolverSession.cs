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
        private readonly ConcurrentBag<IDisposable> disposables = new ConcurrentBag<IDisposable>();
        private volatile bool isDisposed = false;

        public IResolver Resolver => resolver;

        public ResolverSession(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public bool Contains<T>(bool parent)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            return resolver.Contains<T>(parent);
        }

        public bool Contains(Type type, bool parent)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            return resolver.Contains(type, parent);
        }

        public void Dispose()
        {
            isDisposed = true;

            while (disposables.Count > 0)
            {
                if (disposables.TryTake(out var dispose))
                    dispose.Dispose();
            }
        }

        public T Resolve<T>()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            var item = resolver.Resolve<T>();
            if (!item.IsSingletone)
                disposables.Add(item);

            return item.Result;
        }

        public object Resolve(Type type)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            var item = resolver.Resolve(type);
            if (!item.IsSingletone)
                disposables.Add(item);

            return item.Result;
        }

        public bool TryResolve<T>(out T value)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

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
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

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
