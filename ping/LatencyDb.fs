
module LatencyDb
open System
open Sql
open Models

let initializeDatabase () =
    nonQuery """
        CREATE TABLE IF NOT EXISTS LatencyData (
            Time DATETIME NOT NULL,
            Server TEXT NOT NULL,
            Latency INTEGER
        );
        CREATE INDEX IF NOT EXISTS idx_latency_time_server ON LatencyData (Time, Server);
    """
    |> execute
    |> ignore

// Insert a LatencyPoint into the database
let insert server (point: LatencyPoint) =
    """
        INSERT INTO LatencyData (Time, Server, Latency)
        VALUES (@Time, @Server, @Latency);
    """
    |> nonQuery
    |> withParam "Time" point.Time
    |> withParam "Server" server
    |> withParam "Latency" (Option.toNullable point.Latency)
    |> execute
    |> ignore

let selectFrom server (from: DateTime) =
    let read = reader (fun r -> {
        Time = r.GetDateTime(0)
        Latency = r.GetOption(1, _.GetInt32)
        Avg = r.GetFloat(2)
        Min = r.GetFloat(3)
        Max = r.GetFloat(4) })
        
    read """
        SELECT 
            Time,
            Latency,
            (
                SELECT AVG(Latency)
                FROM LatencyData AS ld2
                WHERE ld2.Server = ld1.Server
                AND ld2.Time BETWEEN DATETIME(ld1.Time, '-5 minutes') AND ld1.Time
            ) AS Avg,
            (
                SELECT MIN(Latency)
                FROM LatencyData AS ld2
                WHERE ld2.Server = ld1.Server
                AND ld2.Time BETWEEN DATETIME(ld1.Time, '-5 minutes') AND ld1.Time
            ) AS Min,
            (
                SELECT MAX(Latency)
                FROM LatencyData AS ld2
                WHERE ld2.Server = ld1.Server
                AND ld2.Time BETWEEN DATETIME(ld1.Time, '-5 minutes') AND ld1.Time
            ) AS Max
        FROM LatencyData AS ld1
        WHERE Server = @Server AND Time >= @from
        ORDER BY Time DESC; """
    |> withParam "Server" server
    |> withParam "from" from
    |> execute

let delete server =
    nonQuery "DELETE FROM LatencyData WHERE Server = @Server"
    |> withParam "Server" server
    |> ignore
