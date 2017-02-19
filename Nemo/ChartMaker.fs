namespace Nemo

open System
open System.IO
open XPlot.Plotly
open FSharp.Data

type BucketChartType =
    | CumValues
    | Cdf
    | Pdf

type BucketChartOptions = {
    Foo : string }

type SimpleChartType =
    | Line
    | CumulativeLine

type SimpleChartOptions = {
    Bar : string }

type TimedChartType =
    | TimedLine
    | CumulativeTimedLine

type TimedChartOptions = {
    Baz : string }

type ChartConfig =
    | BucketChart of BucketChartType * BucketChartOptions option
    | SimpleChart of SimpleChartType * SimpleChartOptions option
    | TimedChart of TimedChartType * TimedChartOptions option

type ChartSpec = {
    Path : DataPath
    Config : ChartConfig }

[<RequireQualifiedAccess>]
module ChartMaker =
    /// Make a trace from some data.
    let private makeSeries chartSpec name data : Trace option =
        match chartSpec with
        | BucketChart (ty,b) ->
            match data with
            | Buckets container ->
                match ty with
                | CumValues ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    container.Buckets
                    |> List.ofArray
                    |> List.rev
                    |> List.scan
                        (fun (x,y) bucket -> (x + bucket.Weight, y + bucket.Weight * bucket.Response))
                        (0.0, 0.0)
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Cdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    container.Buckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / 1000.0) + 0.0005)))
                    |> List.ofArray
                    |> fun l -> (minValue::l)@[maxValue]
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Pdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    container.Buckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / 1000.0) + 0.0005)))
                    |> List.ofArray
                    |> fun l -> l@[maxValue]
                    |> List.scan
                        (fun (_, (lastX, lastY)) (x, y) ->
                            let g = (y - lastY) / (x - lastX)
                            ((x, g), (x, y)))
                        (minValue, minValue)
                    |> List.map fst
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
            | _ -> None
        | SimpleChart (ty,b) ->
            match ty with
            | Line ->
                match data with
                | SimpleData(data) ->
                    Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None
            | CumulativeLine ->
                match data with
                | SimpleData(data) ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    data
                    |> List.scan
                        (fun (_,ty) (x,y) -> (x, ty + y))
                        (0.0, 0.0)
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None
        | TimedChart (ty,b) ->
            match ty with
            | TimedLine ->
                match data with
                | TimedData(data) ->
                    Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None
            | CumulativeTimedLine ->
                match data with
                | TimedData(data) ->
                    data
                    |> List.scan
                        (fun (_,ty) (x,y) -> (x, ty + y))
                        (DateTime.MinValue, 0.0)
                    |> List.skip 1
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None

    /// Make a chart from data.
    let chart chartSpec data =
        let data = data |> TraverseData.filterData chartSpec.Path |> TraverseData.collapseDivergences
        match data with
        | Grouped m ->
            m
            |> Map.map (fun key -> makeSeries chartSpec.Config key)
            |> Map.filter (fun _ -> Option.isSome)
            |> Map.map (fun _ -> Option.get)
        | Invalid -> Map.empty
        | _ ->
            match (makeSeries chartSpec.Config "Data" data) with
            | None -> Map.empty
            | Some trace -> Map.ofList [("Data", trace)]
        |> Map.toList
        |> List.map snd
        |> Chart.Plot

