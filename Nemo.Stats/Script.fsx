#load "../packages/FsLab/FsLab.fsx"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Nemo.Stats.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\MathNet.Numerics.FSharp.dll"
#r @"D:\Projects\Nemo\Nemo.Stats\bin\Debug\Newtonsoft.Json.dll"

open System
open System.IO
open FsLab
open XPlot.Plotly
open Nemo
open Nemo.Stats
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
let bucket ty = {Path=[];Config=BucketChart(ty,None)}
let bucket2 nq ty = {Path=[];Config=BucketChart(ty,(Some {NumQuantiles = (Some nq)}))}
let timed s ty = {Path=[All;Specific s];Config=TimedChart(ty,None)}
let bucketf ty = {Path=[Specific "Buckets"];Config=BucketChart(ty,None)}
let bucket2f nq ty = {Path=[Specific "Buckets"];Config=BucketChart(ty,(Some {NumQuantiles = (Some nq)}))}
let timedf s ty = {Path=[Specific "Daily";All;Specific s];Config=TimedChart(ty,None)}
let spec =
    Manual
        [
            ("Separate",    [Specific "Buckets"],   FromData ([All], (Page [("Cdf", bucket Cdf);("Pdf", bucket Pdf)])))
            ("Together",    [Specific "Buckets"],   (Page   [
                                                                ("Cdf", bucket Cdf)
                                                                ("Pdf", bucket Pdf)
                                                                ("CumValues", bucket CumValues)
                                                                ("PredResp", bucket PredResp)
                                                                ("Cdf", bucket2 Ten Cdf)
                                                                ("Pdf", bucket2 Ten Pdf)
                                                                ("CumValues", bucket2 Ten CumValues)
                                                                ("PredResp", bucket2 Ten PredResp)
                                                            ]))
            ("Daily",       [Specific "Daily"],     (Page   [
                                                                ("Metric1", (timed "Metric1" TimedLine))
                                                                ("Metric1 Cumulative", (timed "Metric1" CumulativeTimedLine))
                                                                ("Metric2", (timed "Metric2" TimedLine))
                                                                ("Metric2 Cumulative", (timed "Metric2" CumulativeTimedLine))
                                                            ]))
            ("OnePage",     [],                     (Page   [
                                                                ("Cdf", bucketf Cdf)
                                                                ("Pdf", bucketf Pdf)
                                                                ("CumValues", bucketf CumValues)
                                                                ("PredResp", bucketf PredResp)
                                                                ("Cdf", bucket2f Ten Cdf)
                                                                ("Pdf", bucket2f Ten Pdf)
                                                                ("CumValues", bucket2f Ten CumValues)
                                                                ("PredResp", bucket2f Ten PredResp)
                                                                ("Metric1", (timedf "Metric1" TimedLine))
                                                                ("Metric1 Cumulative", (timedf "Metric1" CumulativeTimedLine))
                                                                ("Metric2", (timedf "Metric2" TimedLine))
                                                                ("Metric2 Cumulative", (timedf "Metric2" CumulativeTimedLine))
                                                            ]))
        ]
//Report.saveToFile "Report" (DirectoryInfo @"D:\Nemo\Test") "report.html" ({ Layout = spec }) (allData)

let write (file:FileInfo) (data:Data) =
    let serializer = JsonSerializer()
    serializer.Formatting <- Formatting.Indented
    use stream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write)
    use writer = new StreamWriter(stream)
    serializer.Serialize(writer, data, typeof<Data>)
//write (FileInfo @"D:\Nemo\Test\data.json") allData

let chart =
    let data = match allData with | Grouped m -> m.["Buckets"] | _ -> failwith ""
    let path = []
    let config = BucketChart(CumValues, None)
    let spec = { Path = path ; Config = config }
    (ChartMaker.chart spec data).GetHtml()


