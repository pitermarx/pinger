module LatencyData
open System
open Models
open Sql

type private LatencyDb(config: Config) =
    // Create database instance, that has the execute method 
    let db = new SqlLiteDb(config.ConnectionString)

    // initialize schema
    do
        nonQuery """
            CREATE TABLE IF NOT EXISTS LatencyData (
                Time DATETIME NOT NULL,
                Server TEXT NOT NULL,
                Latency INTEGER
            );
            CREATE INDEX IF NOT EXISTS idx_latency_time_server ON LatencyData (Time, Server);
        """
        |> db.execute
        |> ignore

    // add a row to latencyData table
    member _.insert (point: LatencyPoint) =
        nonQuery """
            INSERT INTO LatencyData (Time, Server, Latency)
            VALUES (@Time, @Server, @Latency);
        """
        |> withParam "Time" point.Time
        |> withParam "Server" config.Site
        |> withParam "Latency" (Option.toNullable point.Latency)
        |> db.execute
        |> ignore

    // select with averages
    member _.selectFrom (from: DateTime) =
        let window = $"'-{config.SlidingWindowMinutes} minutes'"
        read $"""
            SELECT 
                Time,
                Latency,
                (
                    SELECT AVG(Latency)
                    FROM LatencyData AS ld2
                    WHERE ld2.Server = ld1.Server
                    AND ld2.Time BETWEEN DATETIME(ld1.Time, {window}) AND ld1.Time
                ) AS Avg,
                (
                    SELECT MIN(Latency)
                    FROM LatencyData AS ld2
                    WHERE ld2.Server = ld1.Server
                    AND ld2.Time BETWEEN DATETIME(ld1.Time, {window}) AND ld1.Time
                ) AS Min,
                (
                    SELECT MAX(Latency)
                    FROM LatencyData AS ld2
                    WHERE ld2.Server = ld1.Server
                    AND ld2.Time BETWEEN DATETIME(ld1.Time, {window}) AND ld1.Time
                ) AS Max
            FROM LatencyData AS ld1
            WHERE Server = @Server AND Time >= @from
            ORDER BY Time DESC; """
        |> map (fun r -> {
            Time = r.GetDateTime(0)
            Latency = r.GetOption(1, _.GetInt32)
            Avg = r.GetFloat(2)
            Min = r.GetFloat(3)
            Max = r.GetFloat(4) })
        |> withParam "Server" config.Site
        |> withParam "from" from
        |> db.execute

    // delete data for a server
    member _.delete () =
        nonQuery "DELETE FROM LatencyData WHERE Server = @Server"
        |> withParam "Server" config.Site
        |> db.execute
        |> ignore

type LatencyData(config: Config) =
    // initialize db and data
    let db = new LatencyDb(config)
    let mutable data = (db.selectFrom (DateTime.Now.AddDays(-1)))

    // given a point and time, calculate new averages
    // assume this point will be the most recent
    let calculate (l: int32) (now: DateTime) =
        let minSlidingWindow = now.AddMinutes(-config.SlidingWindowMinutes)
        let filteredValues =
            data
            |> List.filter (fun d -> d.Time >= minSlidingWindow)
            |> List.choose _.Latency

        let latencies = l :: filteredValues |> List.map float32
        
        { Time = now; Latency = Some l; Max = List.max latencies; Min = List.min latencies; Avg = List.average latencies }

    member _.update last =
        let now = DateTime.Now
        let newPoint =
            match data, last with
            | [], None -> None
            | h :: _, None -> Some({ h with Time = now; Latency = None })
            | _, Some l -> Some(calculate l now)

        if newPoint.IsSome then
            let _2dAgo = now.AddDays(-2)
            data <- newPoint.Value :: data |> List.filter (fun d -> d.Time >= _2dAgo)
            db.insert newPoint.Value

    member _.getData date = data |> List.filter (fun d -> d.Time > date)

    member _.clear () =
        data <- []
        db.delete ()