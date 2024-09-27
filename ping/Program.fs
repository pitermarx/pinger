open Oxpecker
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

[<EntryPoint>]
let main args =
    let appBuilder = WebApplication.CreateBuilder(args)

    let opt = appBuilder.Configuration.GetSection("PingerConfig")
    appBuilder.Services
        .AddSingleton<Pinger.Config>(Pinger.Config.bind opt)
        .AddSingleton<IHostedService, Pinger.PingService>()
        .AddRouting()
        .AddOxpecker() |> ignore

    let app = appBuilder.Build()

    app
        .UseStaticFiles()
        .UseRouting()
        .UseOxpecker(Routes.endpoints) |> ignore

    let conf = app.Services.GetRequiredService<Pinger.Config>()
    LatencyDb.initializeDatabase ()
    LatencyData.init conf.Site

    app.Run()

    0