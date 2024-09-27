module Models
open System

type LatencyPoint = { Time: DateTime; Latency: Option<int32>; Max: float32; Min: float32; Avg: float32 }
type Config = {
     Site: string
     PingInterval: TimeSpan
     SlidingWindowMinutes: int
     ConnectionString: string
}
