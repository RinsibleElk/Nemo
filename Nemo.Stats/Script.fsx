#load "../packages/FsLab/FsLab.fsx"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.Stats.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.FSharp.dll"

open System
open System.IO
open FsLab
open XPlot.Plotly
open Nemo
open Nemo.Stats
open MathNet.Numerics
open MathNet.Numerics.Distributions

let normal mean stdev = Normal.Sample(mean, stdev)

let data mean stdev =
    List.init
        100000
        (fun _ ->
            let x = normal mean stdev
            let weight = max 1.0 (Math.Round(10.0 * Math.Exp(normal 0.5 1.1)))
            (x, weight))
    |> Quantiles.quantiles
    |> Buckets
let dailyData mean stdev =
    let rec loop (date:DateTime) =
        if date.Year > 2016 then Seq.empty
        else
            seq {
                yield date
                yield! loop (date.AddDays(1.0)) }
    loop (DateTime(2016,1,1))
    |> Seq.filter (fun date -> date.DayOfWeek <> DayOfWeek.Saturday)
    |> Seq.filter (fun date -> date.DayOfWeek <> DayOfWeek.Sunday)
    |> Seq.toList
    |> List.map (fun date -> (date, normal mean stdev))
    |> TimedData
let allData =
    let buckets =
        [
            ("Foo", data -0.0001 0.0005)
            ("Bar", data 0.0001 0.0010)
        ]
        |> Map.ofList
        |> Grouped
    let dailyData mean1 mean2 =
        [
            ("Metric1", dailyData mean1 25000.0)
            ("Metric2", dailyData mean2 1000.0)
        ]
        |> Map.ofList
        |> Grouped
    let daily =
        [
            ("Base", dailyData 50000.0 500.0)
            ("Div", dailyData 45000.0 460.0)
        ]
        |> Map.ofList
        |> Grouped
    [
        ("Buckets", buckets)
        ("Daily", daily)
    ]
    |> Map.ofList
    |> Grouped
let bucket ty = {Path=[];Config=BucketChart(ty,None)}
let timed s ty = {Path=[All;Specific s];Config=TimedChart(ty,None)}
let spec =
    Manual
        [
            ("Separate",    [Specific "Buckets"],   FromData ([All], (Page [("Cdf", bucket Cdf);("Pdf", bucket Pdf)])))
            ("Together",    [Specific "Buckets"],   (Page [("Cdf", bucket Cdf);("Pdf", bucket Pdf)]))
            ("Daily",       [Specific "Daily"],     (Page   [
                                                                ("Metric1", (timed "Metric1" TimedLine))
                                                                ("Metric1 Cumulative", (timed "Metric1" CumulativeTimedLine))
                                                                ("Metric2", (timed "Metric2" TimedLine))
                                                                ("Metric2 Cumulative", (timed "Metric2" CumulativeTimedLine))
                                                            ]))
        ]
Report.saveToFile "Report" (DirectoryInfo @"D:\Nemo\Test") "report.html" ({ Layout = spec }) (allData)

