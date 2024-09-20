module LatencyData
    open System

    type LatencyPoint = { Time: DateTime; Latency: Option<int64>; Max: int64; Min: int64; Avg: float }

    let slidingWindowMinutes = 5.0
    let mutable private data = []
    let private calculate (l: int64) (now: DateTime) =
        let minSlidingWindow = now.AddMinutes(-1.0 * slidingWindowMinutes)
        let filteredValues =
            data
            |> List.filter (fun d -> d.Time >= minSlidingWindow)
            |> List.choose _.Latency

        let latencies = l :: filteredValues
        
        { Time = now; Latency = Some l; Max = List.max latencies; Min = List.min latencies; Avg = List.averageBy float latencies }

    let update last =
        let now = DateTime.Now
        data <- 
            match data, last with
            | [], None -> data
            | h :: _, None -> { h with Time = now; Latency = None } :: data
            | _, Some l -> (calculate l now) :: data

    let getData () = data