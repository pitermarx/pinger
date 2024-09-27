module Sql
open System.Data.Common
open System.Data.SQLite

type SqlParam = { Name: string; Value: obj }

let private connectionString = "Data Source=latency.db;Version=3;"

type DbDataReader with
  member x.GetOption(index, f: (DbDataReader -> int32 -> 't)) = 
    if x.IsDBNull(index) then None else Some(f x index)

let withParam name (value: 'T) f (cmd: DbCommand) =
    let param = cmd.CreateParameter()
    param.ParameterName <- name
    param.Value <- value
    cmd.Parameters.Add(param) |> ignore
    f cmd

let execute (f: DbCommand -> 'a) =
    let c = new SQLiteConnection(connectionString)
    c.Open()
    use cmd = c.CreateCommand()
    f cmd

let nonQuery s (cmd: DbCommand) =
    cmd.CommandText <- s
    cmd.ExecuteNonQuery()

let reader read s (cmd: DbCommand) =
    cmd.CommandText <- s
    use reader = cmd.ExecuteReader()
    [ while reader.Read() do yield read reader ]