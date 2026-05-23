using System;
using BeyondNet.Bootstrapper.Interface;
using LightInject;

namespace BeyondNet.Bootstrapper.LightInject
{
    public class LightInjectBootStrapper : IBootstrapper<ServiceContainer>
    {
        private readonly Action<ServiceContainer> _action;

        private readonly ContainerOptions _options;

        public LightInjectBootStrapper(Action<ServiceContainer> action=null, ContainerOptions options=null)
        {
            _action = action;

            _options = options;
        }

        public void Run()
        {
            var container = (_options == null) ? new ServiceContainer() : new ServiceContainer(_options);

            _action?.Invoke(container);

            Result = container;
        }

        public ServiceContainer Result { get; private set; }
    }
}