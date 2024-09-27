module Sql
open System.Data.Common
open System.Data.SQLite
open System.Data

type IDataRecord with
  member x.GetOption(index, f: (IDataRecord -> int32 -> 't)) = 
    if x.IsDBNull(index) then None else Some(f x index)

let withParam name (value: 'T) f (cmd: DbCommand) =
    let param = cmd.CreateParameter()
    param.ParameterName <- name
    param.Value <- value
    cmd.Parameters.Add(param) |> ignore
    f cmd
    
let nonQuery s (cmd: DbCommand) =
    cmd.CommandText <- s
    cmd.ExecuteNonQuery()

let read s (mapper: IDataRecord -> 'a) (cmd: DbCommand) =
    cmd.CommandText <- s
    use reader = cmd.ExecuteReader()
    [ while reader.Read() do yield mapper reader]

let map (mapper: IDataRecord -> 'i) (apply: (IDataRecord -> 'i) -> DbCommand -> 'i list) =
    apply (fun r -> mapper r)

type SqlLiteDb(cnxStr: string) =
    member _.execute (f: DbCommand -> 'a) =
        let c = new SQLiteConnection(cnxStr)
        c.Open()
        use cmd = c.CreateCommand()
        f cmd