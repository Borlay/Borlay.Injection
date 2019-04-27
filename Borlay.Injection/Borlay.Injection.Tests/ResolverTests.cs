using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Borlay.Injection.Tests
{
    [TestClass]
    public class ResolverTests
    {
        [TestMethod]
        public void CreateInstance()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            var toResolve = resolver.CreateSession().CreateInstance<ToResolve>();
            Assert.IsNotNull(toResolve);
            Assert.IsNotNull(toResolve.Resolver);
            Assert.IsNotNull(toResolve.ToResolve2);
            Assert.IsNotNull(toResolve.ToResolve2.Resolver);
            Assert.IsNotNull(toResolve.ToResolve2.ToResolve4);
        }

        [TestMethod]
        public void CreateManyInstanceSingleSession()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();

            Stopwatch watch = Stopwatch.StartNew();

            using (var session = resolver.CreateSession())
            {
                for (int i = 0; i < 100000; i++)
                {
                    var toResolve = session.CreateInstance<ToResolve>();
                }
            }

            resolver.Dispose();

            watch.Stop();

        }

        [TestMethod]
        public void CreateManyInstanceManySession()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                using (var session = resolver.CreateSession())
                {
                    var toResolve = session.CreateInstance<ToResolve>();
                }
            }

            resolver.Dispose();

            watch.Stop();

        }

        [TestMethod]
        public void DisposeNotSingletoneWithSingletone()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            ToResolve toResolve;
            using (var session = resolver.CreateSession())
            {
                toResolve = session.Resolve<ToResolve>();
            }

            Assert.IsTrue(toResolve.IsDisposed);
            Assert.IsFalse(toResolve.ToResolve2.IsDisposed);

            resolver.Dispose();

            Assert.IsTrue(toResolve.ToResolve2.IsDisposed);
        }

        [TestMethod]
        public void DisposeNotSingletoneWithNotSingletone()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            ToResolve3 toResolve;
            using (var session = resolver.CreateSession())
            {
                toResolve = session.Resolve<ToResolve3>();
            }

            Assert.IsTrue(toResolve.IsDisposed);
            Assert.IsTrue(toResolve.ToResolve4.IsDisposed);

            resolver.Dispose();
        }

        [TestMethod]
        public void DisposeSingletoneWithNotSingletone()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            ToResolve2 toResolve;
            using (var session = resolver.CreateSession())
            {
                toResolve = session.Resolve<ToResolve2>();
            }

            Assert.IsFalse(toResolve.IsDisposed);
            Assert.IsFalse(toResolve.ToResolve4.IsDisposed);

            resolver.Dispose();

            Assert.IsTrue(toResolve.IsDisposed);
            Assert.IsTrue(toResolve.ToResolve4.IsDisposed);
        }

        [TestMethod]
        public void RegisterProviderAndGet()
        {
            var resolver = new Resolver();
            resolver.Register((s) => new Tuple<ToResolve4, Action>(new ToResolve4(), null), true);
            var toResolve = resolver.Resolve<IToResolve4>();
            Assert.IsNotNull(toResolve);
        }

        [TestMethod]
        public void RegisterInstanceAndGet()
        {
            var resolver = new Resolver();
            resolver.Register(new ToResolve4(), true);
            var toResolve = resolver.Resolve<IToResolve4>();
            Assert.IsNotNull(toResolve);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void RegisterInstanceAndThrowInherit()
        {
            var resolver = new Resolver();
            resolver.Register(new ToResolve4(), false);
            var toResolve = resolver.Resolve<IToResolve4>();
            Assert.IsNotNull(toResolve);
        }

        [TestMethod]
        public void GetNotSingletone()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            var toResolve = resolver.CreateSession().Resolve<ToResolve>();
            Assert.IsNotNull(toResolve);

            toResolve.Index = 2;

            toResolve = resolver.CreateSession().Resolve<ToResolve>();
            Assert.IsNotNull(toResolve);
            Assert.AreEqual(0, toResolve.Index);
        }

        [TestMethod]
        public void GetSingletone()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            var toResolve = resolver.CreateSession().Resolve<ToResolve2>();
            Assert.IsNotNull(toResolve);

            toResolve.Index = 2;

            toResolve = resolver.CreateSession().Resolve<ToResolve2>();
            Assert.IsNotNull(toResolve);
            Assert.AreEqual(2, toResolve.Index);
        }

        [TestMethod]
        public void GetInherit()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            var toResolve = resolver.CreateSession().Resolve<BaseResolve>();
            Assert.IsNotNull(toResolve);
            Assert.IsInstanceOfType(toResolve, typeof(ToResolve3));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void GetInheritThrow()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<ResolverTests>();
            var toResolve = resolver.CreateSession().Resolve<NotIncludedResolve>();
            Assert.IsNotNull(toResolve);
            Assert.IsInstanceOfType(toResolve, typeof(ToResolve4));
        }

        [TestMethod]
        public void GetFromParent()
        {
            var parent = new Resolver();
            var resolver = new Resolver(parent);


            parent.LoadFromReference<ResolverTests>();

            var toResolve = resolver.GetSingletoneInstance<ToResolve2>();
            Assert.IsNotNull(toResolve);

            toResolve.Index = 5;

            toResolve = parent.GetSingletoneInstance<ToResolve2>();
            Assert.IsNotNull(toResolve);
            Assert.AreEqual(5, toResolve.Index);

            toResolve = parent.CreateSession().Resolve<ToResolve2>();
            Assert.IsNotNull(toResolve);
            Assert.AreEqual(5, toResolve.Index);

            toResolve = resolver.CreateSession().Resolve<ToResolve2>();
            Assert.IsNotNull(toResolve);
            Assert.AreEqual(5, toResolve.Index);
        }
    }

    public class BaseResolve : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;

        public void Dispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            IsDisposed = true;
        }
    }

    public class BaseResolve2 : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;

        public void Dispose()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.GetType().Name);

            IsDisposed = true;
        }
    }

    public class NotIncludedResolve
    {

    }

    [Resolve(IncludeBase = true)]
    public class ToResolve : BaseResolve
    {
        public IResolver Resolver { get; }
        public ToResolve2 ToResolve2 { get; }

        public int Index { get; set; }

        public ToResolve(IResolver resolver, ToResolve2 toResolve2)
        {
            this.Resolver = resolver;
            this.ToResolve2 = toResolve2;
            this.Index = 0;
        }
    }


    [Resolve(Singletone = true, IncludeBase = false)]
    public class ToResolve2 : BaseResolve2
    {
        public IResolver Resolver { get; }
        public ToResolve4 ToResolve4 { get; }

        public int Index { get; set; }

        public ToResolve2(IResolver resolver, ToResolve4 toResolve3)
        {
            this.Resolver = resolver;
            this.ToResolve4 = toResolve3;
            this.Index = 0;
        }
    }

    [Resolve(IncludeBase = true, Priority = 1)]
    public class ToResolve3 : BaseResolve
    {
        public IResolver Resolver { get; }
        public ToResolve4 ToResolve4 { get; }

        public int Index { get; set; }

        public ToResolve3(IResolver resolver, ToResolve4 toResolve4)
        {
            this.Resolver = resolver;
            this.ToResolve4 = toResolve4;
            this.Index = 0;
        }
    }

    public interface IToResolve4
    {

    }

    [Resolve]
    public class ToResolve4 : BaseResolve2, IToResolve4
    {
    }
}
