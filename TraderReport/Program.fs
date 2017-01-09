namespace TraderReport.UI
module UI =

    open System
    open System.Threading
    open System.Threading.Tasks

    open FSharp.Data
    open FSharp.Configuration

    open Services
  
    open TraderReport.Csv
    open Csv
    open TraderReport.Async
    open Async
    open TraderReport.Log
    open Log

    type Settings = AppSettings<"app.config">

    [<EntryPoint>]
    let main argv = 
     
        let service = new PowerService()

        let repeat: (unit -> Async<unit>) = fun () -> 
            async {
                let mutable success = false
                while not success do // todo: add counter to stop
                    try 
                        let dateTimeNow = DateTime.Now
                        let dateText = dateTimeNow.ToString("yyyyMMdd_HHmm")    
                        let utc = TimeZoneInfo.ConvertTimeToUtc(
                            DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0, DateTimeKind.Unspecified).Date.AddHours(-1.0),
                            TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"))

                        let! trades = service.GetTradesAsync dateTimeNow.Date |> Async.AwaitTask // todo: add timeout = 1 minute to cancel: fssnip.net/hx
                        // todo: use AsyncSeq to skip Async.AwaitTask; single pipeline
                        trades
                        |> Seq.collect ( fun x -> x.Periods )
                        |> Seq.groupBy ( fun x -> x.Period )
                        |> Seq.map ( fun (key, values) -> 
                                        ( key, values |> Seq.sumBy ( fun a -> a.Volume )))
                        |> Seq.map ( fun (key, values) -> 
                                        ( utc.AddHours((float)(key - 1)).ToString("HH:mm") , values))
                        |> Seq.csv "\t" ( fun columnName  ->
                                            match columnName with
                                            | "0" -> "Local Time"
                                            | "1" -> "Volume"
                                            | _ -> columnName)
                        |> Seq.write(String.concat "" [ Settings.CsvFilePath;"PowerPosition"; "_"; dateText; ".csv"])
                
                        success <- true
                    with
                    | _  as exn -> error exn;
            }
    
        Async.RunSynchronously(DoPeriodicWork repeat (Settings.DelayInMins * 1000 * 60) (new CancellationTokenSource()).Token)

        0
