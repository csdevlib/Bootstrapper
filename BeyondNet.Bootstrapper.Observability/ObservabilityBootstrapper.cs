using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using BeyondNet.Bootstrapper.Interface;

namespace BeyondNet.Bootstrapper.Observability
{
    public class ObservabilityBootstrapper : IBootstrapper<IServiceCollection>
    {
        private readonly ObservabilityConfiguration _configuration;
        private readonly Action<IServiceCollection>? _action;

        public ObservabilityBootstrapper(IServiceCollection services, ObservabilityConfiguration configuration, Action<IServiceCollection>? action = null)
        {
            Result = services;
            _configuration = configuration;
            _action = action;
        }

        public void Run()
        {
            // Configure Serilog to use OTLP
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = _configuration.OTLPEndpoint ?? "http://localhost:4317";
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        { "service.name", _configuration.ServiceName ?? "UnknownService" },
                        { "service.version", _configuration.ServiceVersion ?? "1.0.0" }
                    };

                    if (_configuration.ResourceAttributes != null)
                    {
                        foreach (var attr in _configuration.ResourceAttributes)
                        {
                            options.ResourceAttributes[attr.Key] = attr.Value;
                        }
                    }
                })
                .CreateLogger();

            // Configure OpenTelemetry Tracing via DI
            if (Result != null)
            {
                Result.AddOpenTelemetry().WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                            serviceName: _configuration.ServiceName ?? "UnknownService",
                            serviceVersion: _configuration.ServiceVersion ?? "1.0.0"))
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(_configuration.OTLPEndpoint ?? "http://localhost:4317");
                        });
                });
            }

            _action?.Invoke(Result!);
        }

        public IServiceCollection? Result { get; private set; }
    }
}
