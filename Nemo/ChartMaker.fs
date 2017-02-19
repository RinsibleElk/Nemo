namespace Nemo

open System
open System.IO
open XPlot.Plotly
open FSharp.Data

type BucketChartType =
    | CumValues
    | PredResp
    | Cdf
    | Pdf

/// By restricting this using types, we get to have a slightly easier time of things.
type NumQuantiles =
    | Four
    | Five
    | Ten
    | OneHundred

[<RequireQualifiedAccess>]
module internal BucketChartPreparation =
    let private collapseFromExactMult numQuantiles (buckets:Bucket[]) =
        let factor = 1000 / numQuantiles
        let halfFactor = (factor / 2) - 1
        [| 0 .. (numQuantiles - 1) |]
        |> Array.map
            (fun i ->
                let median = buckets.[i * factor + halfFactor].Max
                let max = buckets.[(i + 1) * factor - 1].Max
                let min = buckets.[i * factor].Min
                let (sum, sumSquares, weight, y) =
                    [(i * factor) .. ((i + 1) * factor - 1)]
                    |> List.fold
                        (fun (s, ss, w, y) j ->
                            let bucket = buckets.[j]
                            (s + bucket.Sum, ss + bucket.SumSquares, w + bucket.Weight, y + bucket.Response))
                        (0.0,0.0,0.0,0.0)
                {
                    Weight = weight
                    Response = y  
                    Sum = sum
                    SumSquares = sumSquares
                    Median = median
                    Max = max
                    Min = min
                })
    let collapse numQuantiles (buckets:Bucket[]) =
        match numQuantiles with
        | Four -> collapseFromExactMult 4 buckets
        | Five -> collapseFromExactMult 5 buckets
        | Ten -> collapseFromExactMult 10 buckets
        | OneHundred -> collapseFromExactMult 100 buckets

type BucketChartOptions = {
    NumQuantiles : NumQuantiles option }

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
            let numQuantiles = b |> Option.map (fun x -> x.NumQuantiles) |> defaultArg <| None
            let n = match numQuantiles with | None -> 1000 | Some x -> match x with | Four -> 4 | Five -> 5 | Ten -> 10 | OneHundred -> 100
            let mapBuckets = numQuantiles |> Option.map BucketChartPreparation.collapse |> defaultArg <| id
            match data with
            | Buckets container ->
                match ty with
                | CumValues ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    container.Buckets
                    |> mapBuckets
                    |> List.ofArray
                    |> List.rev
                    |> List.scan
                        (fun (x,y) bucket -> (x + bucket.Weight, y + bucket.Response))
                        (0.0, 0.0)
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | PredResp ->
                    container.Buckets
                    |> mapBuckets
                    |> List.ofArray
                    |> List.map (fun bucket -> (bucket.Sum / bucket.Weight, bucket.Response / bucket.Weight))
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Cdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    let fn = float n
                    let m = 1.0 / (2.0 * fn)
                    container.Buckets
                    |> mapBuckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / fn) + m)))
                    |> List.ofArray
                    |> fun l -> (minValue::l)@[maxValue]
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Pdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    let fn = float n
                    let m = 1.0 / (2.0 * fn)
                    container.Buckets
                    |> mapBuckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / fn) + m)))
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

