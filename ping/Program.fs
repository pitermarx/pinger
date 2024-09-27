open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open Models
open Microsoft.AspNetCore.Builder
open Oxpecker
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open System.Net.NetworkInformation


let config = {
     Site = "google.com"
     PingInterval = TimeSpan.FromSeconds(5) // seconds
     SlidingWindowMinutes = 5 // minutes
     ConnectionString = "Data Source=latency.db;Version=3;"
}

let data = new LatencyData.LatencyData(config)

let buildPinger (s: IServiceProvider) =
    let log = s.GetService<ILoggerFactory>().CreateLogger("Pinger")
    fun (site: string) ->
        use ping = new Ping()
        let reply = ping.Send(config.Site)
        match reply.Status with
        | IPStatus.Success ->
            log.LogInformation ("Ping to {Server} successful: {Time} ms", site, reply.RoundtripTime)
            Some (int32 reply.RoundtripTime)
        | _ ->
            log.LogWarning ("Ping to {Server} failed", site)
            None
        


[<EntryPoint>]
let main args =
    let appBuilder = WebApplication.CreateBuilder(args)

    appBuilder.Services
        .AddSingleton(data)
        .AddSingleton<IHostedService>(fun s ->
            let pinger = buildPinger s 
            let log = s.GetService<ILoggerFactory>().CreateLogger("PingService")
            {
                new BackgroundService() with
                member _.ExecuteAsync (ct: CancellationToken) = 
                    task {
                        while not ct.IsCancellationRequested do
                            try
                                data.update <| pinger config.Site
                            with
                            | ex ->
                                log.LogError(ex, "Failed to add latency entry")

                            do! Task.Delay(config.PingInterval, ct)
                    }
            } :> IHostedService)
        .AddRouting()
        .AddOxpecker() |> ignore

    let app = appBuilder.Build()

    app
        .UseStaticFiles()
        .UseRouting()
        .UseOxpecker([
            GET [
                route "/" <| htmlView (Routes.chart config)
                routef "/data/{%f}" <| Routes.getData
            ]
        ]) |> ignore

    app.Run()
    0