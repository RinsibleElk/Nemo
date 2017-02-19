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
let allData =
    [
        ("Foo", data -0.0001 0.0005)
        ("Bar", data 0.0001 0.0010)
    ]
    |> Map.ofList
    |> Grouped
let spec =
    Manual
        (Map.ofList
            [
                ("Separate", ([], FromData ([All], (Page [("Cdf", BucketChart(Cdf,None));("Pdf", BucketChart(Pdf,None))]))))
                ("Together", ([], (Page [("Cdf", BucketChart(Cdf,None));("Pdf", BucketChart(Pdf,None))])))
            ])
ChartMaker.saveToFile "Report" (DirectoryInfo @"D:\Nemo\Test") "report.html" ({ Layout = spec }) (allData)

