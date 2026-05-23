using System;
using Microsoft.Extensions.DependencyInjection;
using BeyondNet.Bootstrapper.Interface;

namespace BeyondNet.Bootstrapper.DependencyInjection
{
    public class DependencyInjectionBootstrapper : IBootstrapper<IServiceCollection>
    {
        private readonly Action<IServiceCollection>? _action;

        public DependencyInjectionBootstrapper(Action<IServiceCollection>? action = null)
        {
            _action = action;
        }

        public DependencyInjectionBootstrapper(IServiceCollection services, Action<IServiceCollection>? action = null)
        {
            _action = action;
            Result = services;
        }

        public void Run()
        {
            if (Result == null)
            {
                Result = new ServiceCollection();
            }

            _action?.Invoke(Result);
        }

        public IServiceCollection? Result { get; private set; }
    }
}
