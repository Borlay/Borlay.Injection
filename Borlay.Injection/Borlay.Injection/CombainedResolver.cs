using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Borlay.Injection
{
    public class CombainedResolver : IResolver
    {
        private readonly IResolver[] resolvers;

        public CombainedResolver(params IResolver[] resolvers)
        {
            this.resolvers = resolvers;
        }

        private bool Any(Func<IResolver, bool>  any)
        {
            for (int i = 0; i < resolvers.Length; i++)
            {
                if (any(resolvers[i]))
                    return true;
            }
            return false;
        }

        public bool Contains<T>(bool parent)
        {
            return Any(r => r.Contains<T>(parent));
        }

        public bool Contains(Type type, bool parent)
        {
            return Any(r => r.Contains(type, parent));
        }

        public ResolverItem<T> Resolve<T>()
        {
            ResolverItem<T> value = null;
            if (Any(r => r.TryResolve<T>(out value)))
                return value;

            throw new KeyNotFoundException($"Instance for type '{typeof(T).Name}' not found");
        }

        public ResolverItem<object> Resolve(Type type)
        {
            ResolverItem<object> value = null;
            if (Any(r => r.TryResolve(type, out value)))
                return value;

            throw new KeyNotFoundException($"Instance for type '{type.Name}' not found");
        }

        public bool TryResolve(Type type, out ResolverItem<object> value)
        {
            value = null;
            ResolverItem<object> val = null;
            if (Any(r => r.TryResolve(type, out val)))
            {
                value = val;
                return true;
            }
            return false;
        }

        public bool TryResolve<T>(out ResolverItem<T> value)
        {
            value = null;
            ResolverItem<T> val = null;
            if (Any(r => r.TryResolve<T>(out val)))
            {
                value = val;
                return true;
            }
            return false;
        }

        public IResolverSession CreateSession()
        {
            return new ResolverSession(this);
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}
