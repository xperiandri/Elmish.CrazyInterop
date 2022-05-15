module ElmishCrazyInterop.UnoHost

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open ElmishCrazyInterop.Programs

let configureServices (ctx : HostBuilderContext) (services : IServiceCollection) =
    services.AddScoped<App.Program>() |> ignore

[<CompiledName("CreateDefaultBuilder")>]
let createDefaultBuilder () =

    let environmentName = Environments.Development

    HostBuilder()
        .UseEnvironment(environmentName)
        .ConfigureServices(configureServices)

[<CompiledName("ElmConfig")>]
let elmConfig = { Elmish.Uno.ElmConfig.Default
                  with LogConsole = System.Diagnostics.Debugger.IsAttached }
