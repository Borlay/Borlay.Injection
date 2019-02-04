using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    public interface IResolver : IDisposable
    {
        bool Contains<T>(bool parent);
        bool Contains(Type type, bool parent);

        bool TryResolve<T>(out ResolverItem<T> value);
        bool TryResolve(Type type, out ResolverItem<object> value);

        ResolverItem<T> Resolve<T>();
        ResolverItem<object> Resolve(Type type);

        IResolverSession CreateSession();
    }
}
