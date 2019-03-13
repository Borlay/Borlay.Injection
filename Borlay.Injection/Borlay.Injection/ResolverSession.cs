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
        private readonly ConcurrentStack<IDisposable> disposables = new ConcurrentStack<IDisposable>();
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
            return disposables.TryDispose(out aggregateException);
        }

        public T Resolve<T>()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            var createFactory = resolver.Resolve<T>();
            var item = createFactory.Create<T>(this);
            if (!item.IsSingletone)
                disposables.Push(item);

            return item.Result;
        }

        public object Resolve(Type type)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            var createFactory = resolver.Resolve(type);
            var item = createFactory.Create(this);
            if (!item.IsSingletone)
                disposables.Push(item);

            return item.Result;
        }

        public bool TryResolve<T>(out T value)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(ResolverSession));

            value = default(T);
            if(resolver.TryResolve<T>(out var createFactory))
            {
                var item = createFactory.Create<T>(this);
                if (!item.IsSingletone)
                    disposables.Push(item);

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
            if (resolver.TryResolve(type, out var createFactory))
            {
                var item = createFactory.Create(this);
                if (!item.IsSingletone)
                    disposables.Push(item);

                value = item.Result;
                return true;
            }

            return false;
        }
    }
}
