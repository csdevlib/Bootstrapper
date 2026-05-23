# BeyondNet.Bootstrapper

Just another library to bootstrap your libraries

Derived from `raulnq/Jal.Bootstrapper`, licensed under Apache-2.0. The
`BeyondNet.Bootstrapper` namespace and package family are maintained in this
repository.

## How to use?

Create your Bootstrapper class
```csharp
public class DoSomethingBootstrapper : IBootstrapper<bool>
{
    public void Run()
    {
        Result = true;
    }
    public bool Result { get; private set; }
}
```
Create an instance of your class and add it to the CompositeBootstrapper class
```csharp
var bootstrapper = new DoSomethingBootstrapper();

new CompositeBootstrapper().Add(bootstrapper).Run();
```	
Check the results of your Bootstrapper class looking the property Result
```csharp
var result = bootstrapper.Result;
```	
## Implementations

* CastleWindsor [![NuGet](https://img.shields.io/nuget/v/BeyondNet.Bootstrapper.CastleWindsor.svg)](https://www.nuget.org/packages/BeyondNet.Bootstrapper.CastleWindsor )
* AutoMapper [![NuGet](https://img.shields.io/nuget/v/BeyondNet.Bootstrapper.AutoMapper.svg)](https://www.nuget.org/packages/BeyondNet.Bootstrapper.AutoMapper )
* LightInject [![NuGet](https://img.shields.io/nuget/v/BeyondNet.Bootstrapper.LightInject.svg)](https://www.nuget.org/packages/BeyondNet.Bootstrapper.LightInject )
* Serilog.Sinks.Splunk [![NuGet](https://img.shields.io/nuget/v/BeyondNet.Bootstrapper.Serilog.Sinks.Splunk.svg)](https://www.nuget.org/packages/BeyondNet.Bootstrapper.Serilog.Sinks.Splunk )
