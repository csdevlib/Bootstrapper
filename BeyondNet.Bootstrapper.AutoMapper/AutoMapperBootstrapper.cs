using System;
using AutoMapper;
using BeyondNet.Bootstrapper.Interface;

namespace BeyondNet.Bootstrapper.AutoMapper
{
    public class AutoMapperBootstrapper : IBootstrapper<MapperConfiguration>
    {
        private readonly Action<IMapperConfigurationExpression>? _action;

        public AutoMapperBootstrapper(Action<IMapperConfigurationExpression>? action = null)
        {
            _action = action;
        }

        public void Run()
        {
            var config = new MapperConfiguration(_action);
            Result = config;
        }

        public MapperConfiguration? Result { get; private set; }
    }
}