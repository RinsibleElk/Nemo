#load "../packages/FsLab/FsLab.fsx"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.Stats.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.Html.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.FSharp.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Newtonsoft.Json.dll"

open System
open System.IO
open FsLab
open XPlot.Plotly
open Nemo
open Nemo.Stats
open Nemo.Html
open MathNet.Numerics
open MathNet.Numerics.Distributions
open Newtonsoft.Json

let normal mean stdev = Normal.Sample(mean, stdev)

let data mean stdev alpha beta =
    List.init
        100000
        (fun _ ->
            let x = normal mean stdev
            let weight = max 1.0 (Math.Round(10.0 * Math.Exp(normal 0.5 1.1)))
            let y = alpha + x * beta + (normal 0.0 stdev)
            (x, weight, y))
    |> Quantiles.predResp
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
            ("Foo", data -0.0001 0.0005 0.0 1.0)
            ("Bar", data 0.0001 0.0010 0.0002 0.9)
            ("Baz", data 0.0001 0.0010 -0.0002 1.1)
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
let makeSpec path ty = { Path=path ; Config=(ty, None) }
let makeSpec1 = makeSpec []
let makeSpecCfg path n ty = {Path=path ; Config=(ty, (Some {NumQuantiles=(Some n)}))}
let makeSpecCfg1 n ty = {Path=[] ; Config=(ty, (Some {NumQuantiles=(Some n)}))}
let spec =
    Manual
        [
            ("Separate",    [Specific "Buckets"],   FromData ([All], (Page [("Cdf", makeSpec1 Cdf);("Pdf", makeSpec1 Pdf)])))
            ("Together",    [Specific "Buckets"],   (Page   [
                                                                ("Cdf", makeSpec1 Cdf)
                                                                ("Pdf", makeSpec1 Pdf)
                                                                ("CumValues", makeSpec1 CumValues)
                                                                ("PredResp", makeSpec1 PredResp)
                                                                ("Cdf", makeSpecCfg1 Ten Cdf)
                                                                ("Pdf", makeSpecCfg1 Ten Pdf)
                                                                ("CumValues", makeSpecCfg1 Ten CumValues)
                                                                ("PredResp", makeSpecCfg1 Ten PredResp)
                                                            ]))
            ("Daily",       [Specific "Daily"],     (Page   [
                                                                ("Metric1", (makeSpec [All;Specific "Metric1"] Line))
                                                                ("Metric1 Cumulative", (makeSpec [All;Specific "Metric1"] CumulativeLine))
                                                                ("Metric2", (makeSpec [All;Specific "Metric2"] Line))
                                                                ("Metric2 Cumulative", (makeSpec [All;Specific "Metric2"] CumulativeLine))
                                                            ]))
            ("OnePage",     [],                     (Page   [
                                                                ("Cdf", makeSpec [Specific "Buckets"] Cdf)
                                                                ("Pdf", makeSpec [Specific "Buckets"] Pdf)
                                                                ("CumValues", makeSpec [Specific "Buckets"] CumValues)
                                                                ("PredResp", makeSpec [Specific "Buckets"] PredResp)
                                                                ("Cdf", makeSpecCfg [Specific "Buckets"] Ten Cdf)
                                                                ("Pdf", makeSpecCfg [Specific "Buckets"] Ten Pdf)
                                                                ("CumValues", makeSpecCfg [Specific "Buckets"] Ten CumValues)
                                                                ("PredResp", makeSpecCfg [Specific "Buckets"] Ten PredResp)
                                                                ("Metric1", (makeSpec [Specific "Daily";All;Specific "Metric1"] Line))
                                                                ("Metric1 Cumulative", (makeSpec [Specific "Daily";All;Specific "Metric1"] CumulativeLine))
                                                                ("Metric2", (makeSpec [Specific "Daily";All;Specific "Metric2"] Line))
                                                                ("Metric2 Cumulative", (makeSpec [Specific "Daily";All;Specific "Metric2"] CumulativeLine))
                                                            ]))
        ]

File.WriteAllText(@"D:\Nemo\Test\report2.html", ReportWriter.makeReportHtml ChartType.Plotly "Report" {Layout=spec} allData)

