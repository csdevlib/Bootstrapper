# BeyondNet.Bootstrapper

[🇬🇧 English](README.md) | [🇪🇸 Español](README.es.md)

A lightweight, extensible library for orchestrating the startup sequence of any .NET application or library. Based on the **Composite** pattern, it lets you encapsulate each initialization step as an independent, testable unit.

Built on **.NET 10** with full support for `async/await`, `Nullable Reference Types`, and a Cloud Native observability stack.

---

## Table of Contents

1. [Why BeyondNet.Bootstrapper?](#why-beyondnetbootstrapper)
2. [Installation](#installation)
3. [Core Concepts](#core-concepts)
4. [Quick Start — Synchronous](#quick-start--synchronous)
5. [Quick Start — Asynchronous](#quick-start--asynchronous)
6. [Adapters](#adapters)
   - [Dependency Injection](#dependency-injection-adapter)
   - [AutoMapper](#automapper-adapter)
   - [Observability](#observability-adapter)
7. [Real-World Example — Combining All Adapters](#real-world-example--combining-all-adapters)
8. [Glossary](#glossary)

---

## Why BeyondNet.Bootstrapper?

Without a standard, startup code tends to become a monolithic block in `Program.cs` that is hard to test and maintain. This library solves that by enforcing a single rule:

> **Each initialization concern lives in its own class. The Composite runs them in order.**

Benefits:
- Each bootstrapper is independently unit-testable.
- The startup sequence is explicit and readable.
- Adding or removing a step never changes surrounding code.
- Async I/O at startup cannot deadlock the application.

---

## Installation

Install only the packages you need:

```bash
# Core (always required)
dotnet add package BeyondNet.Bootstrapper

# Official adapters (add as needed)
dotnet add package BeyondNet.Bootstrapper.DependencyInjection
dotnet add package BeyondNet.Bootstrapper.AutoMapper
dotnet add package BeyondNet.Bootstrapper.Observability
```

---

## Core Concepts

```
IBootstrapper          ← synchronous contract
IBootstrapper<T>       ← synchronous contract with typed result
IBootstrapperAsync     ← async contract with CancellationToken
IBootstrapperAsync<T>  ← async contract with typed result

CompositeBootstrapper      ← runs IBootstrapper list in order
CompositeBootstrapperAsync ← runs IBootstrapperAsync list in order, respects cancellation
```

**Flow:**

```
App startup
  └─ CompositeBootstrapper / CompositeBootstrapperAsync
        ├─ Step 1: DatabaseBootstrapper.Run()
        ├─ Step 2: AutoMapperBootstrapper.Run()
        └─ Step 3: ObservabilityBootstrapper.Run()
```

---

## Quick Start — Synchronous

### Step 1 — Implement `IBootstrapper<T>`

```csharp
using BeyondNet.Bootstrapper.Interface;

// Encapsulates any initialization logic and exposes the result via Result
public class FeatureFlagBootstrapper : IBootstrapper<bool>
{
    public bool? Result { get; private set; }

    public void Run()
    {
        // Any synchronous setup: read config, validate env vars, etc.
        Result = true;
    }
}
```

### Step 2 — Orchestrate with `CompositeBootstrapper`

```csharp
using BeyondNet.Bootstrapper.Impl;

var featureFlags = new FeatureFlagBootstrapper();

new CompositeBootstrapper()
    .Add(featureFlags)
    .Run();

if (featureFlags.Result == true)
    Console.WriteLine("Feature flags ready.");
```

### Multiple steps in sequence

```csharp
var step1 = new DatabaseBootstrapper(connectionString);
var step2 = new CacheBootstrapper(redisUrl);
var step3 = new FeatureFlagBootstrapper();

new CompositeBootstrapper()
    .Add(step1)
    .Add(step2)
    .Add(step3)
    .Run();
```

---

## Quick Start — Asynchronous

Use the async engine whenever a step requires I/O (database ping, HTTP call, file read).

### Step 1 — Implement `IBootstrapperAsync<T>`

```csharp
using BeyondNet.Bootstrapper.Interface;

public class DatabaseConnectionBootstrapper : IBootstrapperAsync<bool>
{
    private readonly string _connectionString;

    public DatabaseConnectionBootstrapper(string connectionString)
        => _connectionString = connectionString;

    public bool? Result { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Simulate or replace with real DB ping
        await Task.Delay(50, cancellationToken);
        Result = true;
    }
}
```

### Step 2 — Orchestrate with `CompositeBootstrapperAsync`

```csharp
using BeyondNet.Bootstrapper.Impl;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var dbStep = new DatabaseConnectionBootstrapper("Server=localhost;...");

await new CompositeBootstrapperAsync()
    .Add(dbStep)
    .RunAsync(cts.Token);

if (dbStep.Result == true)
    Console.WriteLine("Database connection established.");
```

> **Rule:** If any step requires async, use `CompositeBootstrapperAsync` for all steps. Do not mix sync and async bootstrappers.

---

## Adapters

### Dependency Injection Adapter

**Package:** `BeyondNet.Bootstrapper.DependencyInjection`

Registers services into `IServiceCollection` and exposes the populated collection as the result.

**Minimal example:**

```csharp
using BeyondNet.Bootstrapper.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var diBootstrapper = new DependencyInjectionBootstrapper(services =>
{
    services.AddSingleton<IGreeter, ConsoleGreeter>();
});

diBootstrapper.Run();
```

**Full pipeline — register, build provider, resolve:**

```csharp
using BeyondNet.Bootstrapper.DependencyInjection;
using BeyondNet.Bootstrapper.Impl;
using Microsoft.Extensions.DependencyInjection;

// 1. Create bootstrapper and register dependencies
var diBootstrapper = new DependencyInjectionBootstrapper(services =>
{
    services.AddSingleton<IOrderRepository, SqlOrderRepository>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddLogging();
});

// 2. Run registration
new CompositeBootstrapper()
    .Add(diBootstrapper)
    .Run();

// 3. Build IServiceProvider from the populated collection
var provider = diBootstrapper.Result!.BuildServiceProvider();

// 4. Resolve and use your services
var orderService = provider.GetRequiredService<IOrderService>();
```

**Pass an existing IServiceCollection (e.g., ASP.NET Core):**

```csharp
// In Program.cs — inject into the existing ASP.NET Core collection
var diBootstrapper = new DependencyInjectionBootstrapper(builder.Services, services =>
{
    services.AddSingleton<IPaymentGateway, StripeGateway>();
});

diBootstrapper.Run();
```

---

### AutoMapper Adapter

**Package:** `BeyondNet.Bootstrapper.AutoMapper`

Builds a `MapperConfiguration` and exposes it as the result so you can create `IMapper` instances anywhere in your application.

**Minimal example:**

```csharp
using BeyondNet.Bootstrapper.AutoMapper;

var mapperBootstrapper = new AutoMapperBootstrapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>();
});

mapperBootstrapper.Run();
```

**Full pipeline — configure, build, map:**

```csharp
using BeyondNet.Bootstrapper.AutoMapper;
using BeyondNet.Bootstrapper.Impl;
using AutoMapper;

var mapperBootstrapper = new AutoMapperBootstrapper(cfg =>
{
    cfg.CreateMap<ProductEntity, ProductDto>()
       .ForMember(dest => dest.FullName,
                  opt => opt.MapFrom(src => $"{src.Brand} {src.Model}"));

    cfg.CreateMap<OrderEntity, OrderSummaryDto>();
});

new CompositeBootstrapper()
    .Add(mapperBootstrapper)
    .Run();

// Build the mapper — do this once and store it (singleton)
IMapper mapper = mapperBootstrapper.Result!.CreateMapper();

// Use it anywhere
var dto = mapper.Map<ProductDto>(productEntity);
```

---

### Observability Adapter

**Package:** `BeyondNet.Bootstrapper.Observability`

Configures the **V1 Observability Stack**: structured logs via `Serilog` (OTLP sink) and distributed tracing via `OpenTelemetry`, both pointing to a single OTLP collector.

```
App → ObservabilityBootstrapper → OTLP Collector → Tempo (traces)
                                                  → Loki  (logs)
                                                  → Grafana (dashboards)
```

**Minimal example:**

```csharp
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

new ObservabilityBootstrapper(services, new ObservabilityConfiguration
{
    ServiceName    = "OrderService",
    ServiceVersion = "2.0.0",
    OTLPEndpoint   = "http://localhost:4317"
}).Run();
```

**Full configuration with all options:**

```csharp
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;

var config = new ObservabilityConfiguration
{
    ServiceName    = "PaymentService",
    ServiceVersion = "1.4.2",
    OTLPEndpoint   = "http://otel-collector:4317",

    // Additional resource attributes forwarded to every trace and log
    ResourceAttributes = new Dictionary<string, object>
    {
        { "deployment.environment", "production" },
        { "cloud.region",           "us-east-1"  }
    }
};

var obsBootstrapper = new ObservabilityBootstrapper(services, config);
obsBootstrapper.Run();

// Result exposes the IServiceCollection with OTel tracing registered
var provider = obsBootstrapper.Result!.BuildServiceProvider();
```

> **Local environment:** the `examples/observability/` folder contains a `docker-compose.yml` that spins up an OTel Collector, Tempo, Loki, and Grafana in one command:
> ```bash
> cd examples/observability
> docker-compose up -d
> ```

---

## Real-World Example — Combining All Adapters

The following shows a complete `Program.cs` that wires everything together using a single `CompositeBootstrapper`.

```csharp
using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.DependencyInjection;
using BeyondNet.Bootstrapper.AutoMapper;
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;

// ── 1. Declare each bootstrapper ──────────────────────────────────────
var services = new ServiceCollection();

var di = new DependencyInjectionBootstrapper(services, svc =>
{
    svc.AddSingleton<IOrderRepository, SqlOrderRepository>();
    svc.AddScoped<IOrderService, OrderService>();
});

var mapper = new AutoMapperBootstrapper(cfg =>
{
    cfg.CreateMap<OrderEntity, OrderDto>();
});

var observability = new ObservabilityBootstrapper(services, new ObservabilityConfiguration
{
    ServiceName    = "OrderService",
    ServiceVersion = "2.0.0",
    OTLPEndpoint   = "http://localhost:4317"
});

// ── 2. Run all steps in sequence ─────────────────────────────────────
new CompositeBootstrapper()
    .Add(di)
    .Add(mapper)
    .Add(observability)
    .Run();

// ── 3. Use the results ────────────────────────────────────────────────
var provider   = di.Result!.BuildServiceProvider();
var iMapper    = mapper.Result!.CreateMapper();
var orderSvc   = provider.GetRequiredService<IOrderService>();
```

**Async variant** (e.g., startup with database health-check):

```csharp
using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.Interface;

public class DatabasePingBootstrapper : IBootstrapperAsync<bool>
{
    public bool? Result { get; private set; }
    public async Task RunAsync(CancellationToken ct = default)
    {
        // Replace with actual connection check
        await Task.Delay(20, ct);
        Result = true;
    }
}

// In Program.cs
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

var dbPing = new DatabasePingBootstrapper();

await new CompositeBootstrapperAsync()
    .Add(dbPing)
    .RunAsync(cts.Token);

if (dbPing.Result != true)
    throw new InvalidOperationException("Database unreachable at startup.");
```

---

## Glossary

| Term | Definition |
|---|---|
| **Bootstrapper** | A class that encapsulates one startup concern (DB connection, DI registration, mapping profiles, etc.) and exposes its result via `Result`. |
| **Composite** | Structural design pattern that groups multiple bootstrappers and runs them sequentially as a single unit. |
| **IBootstrapperAsync** | The async contract. Use it whenever a step performs I/O-bound work (network, disk, database) to avoid blocking the thread pool. |
| **CancellationToken** | Passed through `RunAsync` to allow the host to cancel the entire startup sequence if a timeout is exceeded. |
| **OTLP** | OpenTelemetry Protocol — the vendor-neutral format for shipping metrics, traces, and logs to a unified collector. |
| **OTel Collector** | A proxy that receives OTLP signals and fans them out to storage backends (Tempo for traces, Loki for logs). |
