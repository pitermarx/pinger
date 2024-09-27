module Pinger
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.Logging
    open System.Net.NetworkInformation
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.Extensions.Configuration
    
    type Config = {
        Site: string
        PingInterval: int
    } with 
    
        static member bind (config: IConfiguration) =
            let tryParseInt (s: string) =
                match System.Int32.TryParse(s) with
                | true, value -> Some value
                | _ -> None

            {
                Site = config.["Site"] |> Option.ofObj |> Option.defaultValue "google.com"
                PingInterval = config.["PingInterval"] |> tryParseInt |> Option.defaultValue 1
            }
    let private doPing (log: ILogger) (server: string) =
        use ping = new Ping()
        try
            let reply = ping.Send(server)
            match reply.Status with
            | IPStatus.Success ->
                log.LogInformation ("Ping to {Server} successful: {Time} ms", server, reply.RoundtripTime)
                Some (int32 reply.RoundtripTime)
            | _ ->
                log.LogWarning ("Ping to {Server}failed", server)
                None
            
        with
        | ex ->
            log.LogError(ex, "Ping to {Server}", server)
            None
            
    type PingService(log: ILogger<PingService>, config: Config) =
        inherit BackgroundService()
        override _.ExecuteAsync(ct: CancellationToken) : Task =
            task {
                LatencyData.slidingWindowMinutes <- config.PingInterval
                while not ct.IsCancellationRequested do
                    doPing log config.Site |> LatencyData.update
                    do! Task.Delay(config.PingInterval * 1000, ct)
            }
