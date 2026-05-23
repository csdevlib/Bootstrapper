packages\NuGet.CommandLine.3.4.4-rtm-final\tools\nuget pack BeyondNet.Bootstrapper\BeyondNet.Bootstrapper.csproj -Properties "Configuration=Release;Platform=AnyCPU;OutputPath=bin\Release" -Build -IncludeReferencedProjects -OutputDirectory BeyondNet.Bootstrapper.Nuget

packages\NuGet.CommandLine.3.4.4-rtm-final\tools\nuget pack BeyondNet.Bootstrapper.AutoMapper\BeyondNet.Bootstrapper.AutoMapper.csproj -Properties "Configuration=Release;Platform=AnyCPU;OutputPath=bin\Release" -Build -IncludeReferencedProjects -OutputDirectory BeyondNet.Bootstrapper.Nuget

packages\NuGet.CommandLine.3.4.4-rtm-final\tools\nuget pack BeyondNet.Bootstrapper.CastleWindsor\BeyondNet.Bootstrapper.CastleWindsor.csproj -Properties "Configuration=Release;Platform=AnyCPU;OutputPath=bin\Release" -Build -IncludeReferencedProjects -OutputDirectory BeyondNet.Bootstrapper.Nuget

packages\NuGet.CommandLine.3.4.4-rtm-final\tools\nuget pack BeyondNet.Bootstrapper.Serilog.Sinks.Splunk\BeyondNet.Bootstrapper.Serilog.Sinks.Splunk.csproj -Properties "Configuration=Release;Platform=AnyCPU;OutputPath=bin\Release" -Build -IncludeReferencedProjects -OutputDirectory BeyondNet.Bootstrapper.Nuget

packages\NuGet.CommandLine.3.4.4-rtm-final\tools\nuget pack BeyondNet.Bootstrapper.LightInject\BeyondNet.Bootstrapper.LightInject.csproj -Properties "Configuration=Release;Platform=AnyCPU;OutputPath=bin\Release" -Build -IncludeReferencedProjects -OutputDirectory BeyondNet.Bootstrapper.Nuget

pause;