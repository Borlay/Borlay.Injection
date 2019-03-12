using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    public interface IResolver : IDisposable
    {
        bool Contains<T>(bool parent);
        bool Contains(Type type, bool parent);

        bool TryResolve<T>(out ICreateFactory createFactory);
        bool TryResolve(Type type, out ICreateFactory createFactory);

        ICreateFactory Resolve<T>();
        ICreateFactory Resolve(Type type);

        IResolverSession CreateSession();
    }
}
