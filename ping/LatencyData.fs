module LatencyData
    open System
    open Models

    let mutable server = ""
    let mutable slidingWindowMinutes = 5.0
    let mutable private data = []

    let init s =
        server <- s
        data <- (LatencyDb.selectFrom server (DateTime.Now.AddDays(-1)))

    let private calculate (l: int32) (now: DateTime) =
        let minSlidingWindow = now.AddMinutes(-1.0 * slidingWindowMinutes)
        let filteredValues =
            data
            |> List.filter (fun d -> d.Time >= minSlidingWindow)
            |> List.choose _.Latency

        let latencies = l :: filteredValues |> List.map float32
        
        { Time = now; Latency = Some l; Max = List.max latencies; Min = List.min latencies; Avg = List.average latencies }

    let update last =
        let now = DateTime.Now
        let newPoint =
            match data, last with
            | [], None -> None
            | h :: _, None -> Some({ h with Time = now; Latency = None })
            | _, Some l -> Some(calculate l now)

        if newPoint.IsSome then
            let _2dAgo = now.AddDays(-2)
            data <- newPoint.Value :: data |> List.filter (fun d -> d.Time >= _2dAgo)
            LatencyDb.insert server newPoint.Value

    let getData date = data |> List.filter (fun d -> d.Time > date)

    let clear () =
        data <- []
        LatencyDb.delete server