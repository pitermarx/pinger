module Pinger
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.Logging
    open System.Net.NetworkInformation
    open System.Threading
    open System.Threading.Tasks

    let serverUrl = "google.com"
    let pingIntervalSeconds = 5
    let private doPing (log: ILogger) =
        use ping = new Ping()
        try
            let reply = ping.Send(serverUrl)
            match reply.Status with
            | IPStatus.Success ->
                log.LogInformation ("Ping successful: {Time} ms", reply.RoundtripTime)
                Some reply.RoundtripTime
            | _ ->
                log.LogWarning "Ping failed"
                None
            
        with
        | ex ->
            log.LogError(ex, "Ping exception")
            None
            
    type PingService(log: ILogger<PingService>) =
        inherit BackgroundService()
        override _.ExecuteAsync(ct: CancellationToken) : Task =
            task {
                while not ct.IsCancellationRequested do
                    doPing log |> LatencyData.update
                    do! Task.Delay(pingIntervalSeconds * 1000, ct)
            }
