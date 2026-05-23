using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.Tests.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace BeyondNet.Bootstrapper.Tests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void Configure_WithCompositeBootstrapper_ShouldBeTrue()
        {
            var bootstrapper = new DoSomethingBootstrapper();

            new CompositeBootstrapper().Add(bootstrapper).Run();

            bootstrapper.Result.ShouldBe(true);
        }
    }
}
