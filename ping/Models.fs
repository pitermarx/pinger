module Models
open System

type LatencyPoint = { Time: DateTime; Latency: Option<int32>; Max: float32; Min: float32; Avg: float32 }
