using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    public interface IResolverSession : IDisposable
    {
        Resolver Resolver { get; }

        bool Contains<T>(bool parent);
        bool Contains(Type type, bool parent);
        bool TryResolve<T>(out T value);
        bool TryResolve(Type type, out object value);
        T Resolve<T>();

        object Resolve(Type type);

        bool IsDisposed { get; }

        bool TryDispose(out AggregateException aggregateException);

        void AddDisposable(IDisposable disposable);
    }
}
