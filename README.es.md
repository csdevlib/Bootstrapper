# BeyondNet.Bootstrapper

[🇬🇧 English](README.md) | [🇪🇸 Español](README.es.md)

Una librería ligera y extensible para orquestar la secuencia de arranque de cualquier aplicación o librería .NET. Basada en el patrón **Composite**, permite encapsular cada paso de inicialización como una unidad independiente y testeable.

Construida en **.NET 10** con soporte completo para `async/await`, `Nullable Reference Types` y un stack de observabilidad Cloud Native.

---

## Índice

1. [¿Por qué BeyondNet.Bootstrapper?](#por-qué-beyondnetbootstrapper)
2. [Instalación](#instalación)
3. [Conceptos Fundamentales](#conceptos-fundamentales)
4. [Inicio Rápido — Síncrono](#inicio-rápido--síncrono)
5. [Inicio Rápido — Asíncrono](#inicio-rápido--asíncrono)
6. [Adaptadores](#adaptadores)
   - [Dependency Injection](#adaptador-dependency-injection)
   - [AutoMapper](#adaptador-automapper)
   - [Observability](#adaptador-observability)
7. [Ejemplo Real — Combinando Todos los Adaptadores](#ejemplo-real--combinando-todos-los-adaptadores)
8. [Glosario](#glosario)

---

## ¿Por qué BeyondNet.Bootstrapper?

Sin un estándar, el código de arranque tiende a convertirse en un bloque monolítico en `Program.cs` difícil de probar y mantener. Esta librería resuelve ese problema aplicando una sola regla:

> **Cada responsabilidad de inicialización vive en su propia clase. El Composite las ejecuta en orden.**

Beneficios:
- Cada bootstrapper es unitariamente testeable de forma independiente.
- La secuencia de arranque es explícita y legible.
- Agregar o eliminar un paso no modifica el código circundante.
- El I/O asíncrono en el arranque no puede generar deadlocks.

---

## Instalación

Instala únicamente los paquetes que necesitas:

```bash
# Core (siempre requerido)
dotnet add package BeyondNet.Bootstrapper

# Adaptadores oficiales (agregar según necesidad)
dotnet add package BeyondNet.Bootstrapper.DependencyInjection
dotnet add package BeyondNet.Bootstrapper.AutoMapper
dotnet add package BeyondNet.Bootstrapper.Observability
```

---

## Conceptos Fundamentales

```
IBootstrapper          ← contrato síncrono
IBootstrapper<T>       ← contrato síncrono con resultado tipado
IBootstrapperAsync     ← contrato asíncrono con CancellationToken
IBootstrapperAsync<T>  ← contrato asíncrono con resultado tipado

CompositeBootstrapper      ← ejecuta la lista de IBootstrapper en orden
CompositeBootstrapperAsync ← ejecuta la lista de IBootstrapperAsync en orden, respeta cancelación
```

**Flujo:**

```
Inicio de la app
  └─ CompositeBootstrapper / CompositeBootstrapperAsync
        ├─ Paso 1: DatabaseBootstrapper.Run()
        ├─ Paso 2: AutoMapperBootstrapper.Run()
        └─ Paso 3: ObservabilityBootstrapper.Run()
```

---

## Inicio Rápido — Síncrono

### Paso 1 — Implementar `IBootstrapper<T>`

```csharp
using BeyondNet.Bootstrapper.Interface;

// Encapsula cualquier lógica de inicialización y expone el resultado en Result
public class FeatureFlagBootstrapper : IBootstrapper<bool>
{
    public bool? Result { get; private set; }

    public void Run()
    {
        // Cualquier configuración síncrona: leer config, validar variables de entorno, etc.
        Result = true;
    }
}
```

### Paso 2 — Orquestar con `CompositeBootstrapper`

```csharp
using BeyondNet.Bootstrapper.Impl;

var featureFlags = new FeatureFlagBootstrapper();

new CompositeBootstrapper()
    .Add(featureFlags)
    .Run();

if (featureFlags.Result == true)
    Console.WriteLine("Feature flags listos.");
```

### Múltiples pasos en secuencia

```csharp
var paso1 = new DatabaseBootstrapper(connectionString);
var paso2 = new CacheBootstrapper(redisUrl);
var paso3 = new FeatureFlagBootstrapper();

new CompositeBootstrapper()
    .Add(paso1)
    .Add(paso2)
    .Add(paso3)
    .Run();
```

---

## Inicio Rápido — Asíncrono

Usa el motor asíncrono cuando un paso requiera I/O (ping a base de datos, llamada HTTP, lectura de archivo).

### Paso 1 — Implementar `IBootstrapperAsync<T>`

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
        // Reemplaza con un ping real a la base de datos
        await Task.Delay(50, cancellationToken);
        Result = true;
    }
}
```

### Paso 2 — Orquestar con `CompositeBootstrapperAsync`

```csharp
using BeyondNet.Bootstrapper.Impl;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var dbStep = new DatabaseConnectionBootstrapper("Server=localhost;...");

await new CompositeBootstrapperAsync()
    .Add(dbStep)
    .RunAsync(cts.Token);

if (dbStep.Result == true)
    Console.WriteLine("Conexión a base de datos establecida.");
```

> **Regla:** Si algún paso es asíncrono, usa `CompositeBootstrapperAsync` para todos los pasos. No mezcles bootstrappers síncronos y asíncronos.

---

## Adaptadores

### Adaptador Dependency Injection

**Paquete:** `BeyondNet.Bootstrapper.DependencyInjection`

Registra servicios en `IServiceCollection` y expone la colección poblada como resultado.

**Ejemplo mínimo:**

```csharp
using BeyondNet.Bootstrapper.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var diBootstrapper = new DependencyInjectionBootstrapper(services =>
{
    services.AddSingleton<IGreeter, ConsoleGreeter>();
});

diBootstrapper.Run();
```

**Pipeline completo — registrar, construir provider, resolver:**

```csharp
using BeyondNet.Bootstrapper.DependencyInjection;
using BeyondNet.Bootstrapper.Impl;
using Microsoft.Extensions.DependencyInjection;

// 1. Crear el bootstrapper y registrar dependencias
var diBootstrapper = new DependencyInjectionBootstrapper(services =>
{
    services.AddSingleton<IOrderRepository, SqlOrderRepository>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddLogging();
});

// 2. Ejecutar el registro
new CompositeBootstrapper()
    .Add(diBootstrapper)
    .Run();

// 3. Construir IServiceProvider desde la colección poblada
var provider = diBootstrapper.Result!.BuildServiceProvider();

// 4. Resolver y usar tus servicios
var orderService = provider.GetRequiredService<IOrderService>();
```

**Pasar una IServiceCollection existente (ej. ASP.NET Core):**

```csharp
// En Program.cs — inyectar en la colección existente de ASP.NET Core
var diBootstrapper = new DependencyInjectionBootstrapper(builder.Services, services =>
{
    services.AddSingleton<IPaymentGateway, StripeGateway>();
});

diBootstrapper.Run();
```

---

### Adaptador AutoMapper

**Paquete:** `BeyondNet.Bootstrapper.AutoMapper`

Construye un `MapperConfiguration` y lo expone como resultado para que puedas crear instancias de `IMapper` en cualquier parte de la aplicación.

**Ejemplo mínimo:**

```csharp
using BeyondNet.Bootstrapper.AutoMapper;

var mapperBootstrapper = new AutoMapperBootstrapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>();
});

mapperBootstrapper.Run();
```

**Pipeline completo — configurar, construir, mapear:**

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

// Construir el mapper — hacerlo una sola vez y almacenarlo (singleton)
IMapper mapper = mapperBootstrapper.Result!.CreateMapper();

// Usarlo en cualquier parte
var dto = mapper.Map<ProductDto>(productEntity);
```

---

### Adaptador Observability

**Paquete:** `BeyondNet.Bootstrapper.Observability`

Configura el **V1 Observability Stack**: logs estructurados vía `Serilog` (sink OTLP) y trazado distribuido vía `OpenTelemetry`, ambos apuntando a un colector OTLP único.

```
App → ObservabilityBootstrapper → Colector OTLP → Tempo (trazas)
                                                 → Loki  (logs)
                                                 → Grafana (dashboards)
```

**Ejemplo mínimo:**

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

**Configuración completa con todos los atributos:**

```csharp
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;

var config = new ObservabilityConfiguration
{
    ServiceName    = "PaymentService",
    ServiceVersion = "1.4.2",
    OTLPEndpoint   = "http://otel-collector:4317",

    // Atributos de recurso adicionales enviados en cada traza y log
    ResourceAttributes = new Dictionary<string, object>
    {
        { "deployment.environment", "production" },
        { "cloud.region",           "us-east-1"  }
    }
};

var obsBootstrapper = new ObservabilityBootstrapper(services, config);
obsBootstrapper.Run();

// Result expone IServiceCollection con OpenTelemetry registrado
var provider = obsBootstrapper.Result!.BuildServiceProvider();
```

> **Entorno local:** la carpeta `examples/observability/` contiene un `docker-compose.yml` que levanta Colector OTel, Tempo, Loki y Grafana con un solo comando:
> ```bash
> cd examples/observability
> docker-compose up -d
> ```

---

## Ejemplo Real — Combinando Todos los Adaptadores

El siguiente muestra un `Program.cs` completo que conecta todo usando un solo `CompositeBootstrapper`.

```csharp
using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.DependencyInjection;
using BeyondNet.Bootstrapper.AutoMapper;
using BeyondNet.Bootstrapper.Observability;
using Microsoft.Extensions.DependencyInjection;

// ── 1. Declarar cada bootstrapper ─────────────────────────────────────
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

// ── 2. Ejecutar todos los pasos en secuencia ──────────────────────────
new CompositeBootstrapper()
    .Add(di)
    .Add(mapper)
    .Add(observability)
    .Run();

// ── 3. Usar los resultados ────────────────────────────────────────────
var provider   = di.Result!.BuildServiceProvider();
var iMapper    = mapper.Result!.CreateMapper();
var orderSvc   = provider.GetRequiredService<IOrderService>();
```

**Variante asíncrona** (ej. arranque con health-check a base de datos):

```csharp
using BeyondNet.Bootstrapper.Impl;
using BeyondNet.Bootstrapper.Interface;

public class DatabasePingBootstrapper : IBootstrapperAsync<bool>
{
    public bool? Result { get; private set; }
    public async Task RunAsync(CancellationToken ct = default)
    {
        // Reemplaza con una verificación real de conexión
        await Task.Delay(20, ct);
        Result = true;
    }
}

// En Program.cs
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

var dbPing = new DatabasePingBootstrapper();

await new CompositeBootstrapperAsync()
    .Add(dbPing)
    .RunAsync(cts.Token);

if (dbPing.Result != true)
    throw new InvalidOperationException("Base de datos inaccesible al iniciar.");
```

---

## Glosario

| Término | Definición |
|---|---|
| **Bootstrapper** | Clase que encapsula una responsabilidad de inicio (conexión a DB, registro de DI, perfiles de mapeo, etc.) y expone su resultado en `Result`. |
| **Composite** | Patrón de diseño estructural que agrupa múltiples bootstrappers y los ejecuta secuencialmente como una unidad. |
| **IBootstrapperAsync** | Contrato asíncrono. Úsalo cuando un paso realice trabajo I/O-bound (red, disco, base de datos) para no bloquear el thread pool. |
| **CancellationToken** | Pasado a través de `RunAsync` para permitir que el host cancele toda la secuencia de arranque si se excede un timeout. |
| **OTLP** | OpenTelemetry Protocol — formato agnóstico de proveedor para enviar métricas, trazas y logs a un colector unificado. |
| **Colector OTel** | Proxy que recibe señales OTLP y las distribuye a backends de almacenamiento (Tempo para trazas, Loki para logs). |
