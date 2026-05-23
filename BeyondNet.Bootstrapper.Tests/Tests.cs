using System.Threading.Tasks;
using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.Tests.Impl;
using BeyondNet.Bootstrapper.DependencyInjection;
using BeyondNet.Bootstrapper.AutoMapper;
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Linq;

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

        [TestMethod]
        public async Task Configure_WithCompositeBootstrapperAsync_ShouldBeTrue()
        {
            var bootstrapper = new DoSomethingBootstrapperAsync();

            await new CompositeBootstrapperAsync().Add(bootstrapper).RunAsync();

            bootstrapper.Result.ShouldBe(true);
        }

        [TestMethod]
        public void Configure_DependencyInjectionBootstrapper_ShouldRegisterServices()
        {
            var diBootstrapper = new DependencyInjectionBootstrapper(services =>
            {
                services.AddSingleton<IDummyService, DummyService>();
            });

            diBootstrapper.Run();

            diBootstrapper.Result.ShouldNotBeNull();
            diBootstrapper.Result.Count.ShouldBe(1);
            diBootstrapper.Result.First().ServiceType.ShouldBe(typeof(IDummyService));
        }

        [TestMethod]
        public void Configure_AutoMapperBootstrapper_ShouldCreateValidMapper()
        {
            var autoMapperBootstrapper = new AutoMapperBootstrapper(cfg =>
            {
                cfg.CreateMap<DummySource, DummyDestination>();
            });

            autoMapperBootstrapper.Run();

            autoMapperBootstrapper.Result.ShouldNotBeNull();
            var mapper = autoMapperBootstrapper.Result.CreateMapper();
            var result = mapper.Map<DummyDestination>(new DummySource { Name = "Test" });
            result.Name.ShouldBe("Test");
        }

        [TestMethod]
        public void Configure_ObservabilityBootstrapper_ShouldInitializeSuccessfully()
        {
            var config = new ObservabilityConfiguration
            {
                OTLPEndpoint = "http://localhost:4317",
                ServiceName = "TestService"
            };

            var services = new ServiceCollection();
            var obsBootstrapper = new ObservabilityBootstrapper(services, config);
            
            obsBootstrapper.Run();

            obsBootstrapper.Result.ShouldNotBeNull();
            // OpenTelemetry adds multiple services to the collection
            obsBootstrapper.Result.Count.ShouldBeGreaterThan(0);
        }
    }

    public interface IDummyService { }
    public class DummyService : IDummyService { }
    public class DummySource { public string? Name { get; set; } }
    public class DummyDestination { public string? Name { get; set; } }
}
