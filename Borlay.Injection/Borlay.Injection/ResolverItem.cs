using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{

    public class ResolverItem<T> : IDisposable
    {
        public T Result { get; private set; }

        public Action DisposeAction { get; private set; }

        public bool IsSingletone { get; private set; }

        internal ResolverItem(T result, Action disposeAction, bool isSingletone)
        {
            this.Result = result;
            this.DisposeAction = disposeAction;
            this.IsSingletone = isSingletone;
        }

        internal ResolverItem<TAs> As<TAs>()
        {
            return new ResolverItem<TAs>((TAs)(object)Result, DisposeAction, IsSingletone);
        }

        public void Dispose()
        {
            if (!IsSingletone && Result is IDisposable disposable)
                disposable.Dispose();

            DisposeAction?.Invoke();
        }
    }
}
