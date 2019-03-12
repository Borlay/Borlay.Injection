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

        public bool IsDisposed => isDisposed;

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
            if (!TryDispose(out var exception))
                throw exception;
        }

        public bool TryDispose(out AggregateException aggregateException)
        {
            isDisposed = true;

            List<Exception> exceptions = new List<Exception>();
            while (disposables.Count > 0)
            {
                try
                {
                    if (disposables.TryTake(out var dispose))
                        dispose.Dispose();
                }
                catch(Exception e)
                {
                    exceptions.Add(e);
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
