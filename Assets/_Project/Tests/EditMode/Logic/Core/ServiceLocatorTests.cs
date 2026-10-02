using System;
using Enxada.Core;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Core
{
    public class ServiceLocatorTests
    {
        private class FakeService { }

        [SetUp]
        public void SetUp() => ServiceLocator.Clear();

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void Get_ReturnsRegisteredService()
        {
            var service = new FakeService();
            ServiceLocator.Register(service);

            Assert.AreSame(service, ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void Get_ThrowsWhenNotRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void TryGet_ReturnsFalseWhenNotRegistered()
        {
            Assert.IsFalse(ServiceLocator.TryGet<FakeService>(out var service));
            Assert.IsNull(service);
        }

        [Test]
        public void Register_ThrowsOnDuplicate()
        {
            ServiceLocator.Register(new FakeService());

            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Register(new FakeService()));
        }

        [Test]
        public void Register_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<FakeService>(null));
        }

        [Test]
        public void Unregister_RemovesService()
        {
            ServiceLocator.Register(new FakeService());

            Assert.IsTrue(ServiceLocator.Unregister<FakeService>());
            Assert.IsFalse(ServiceLocator.TryGet<FakeService>(out _));
        }
    }
}
