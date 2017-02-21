namespace Nemo

open System
open System.IO
open FSharp.Data

/// By restricting this using types, we get to have a slightly easier time of things.
type NumQuantiles =
    | Four
    | Five
    | Ten
    | OneHundred

/// Not all are interesting for all series types.
type SeriesOptions = {
    NumQuantiles : NumQuantiles option }

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

type SeriesConfig = SeriesType * SeriesOptions option

type SeriesSpec = {
    Path : DataPath
    Config : SeriesConfig }

[<RequireQualifiedAccess>]
module ChartMaker =
    /// Make a trace from some data.
    let private makeSeries (seriesType, seriesOptions) name data : Series option =
        let numQuantiles = seriesOptions |> Option.map (fun x -> x.NumQuantiles) |> defaultArg <| None
        let n = match numQuantiles with | None -> 1000 | Some x -> match x with | Four -> 4 | Five -> 5 | Ten -> 10 | OneHundred -> 100
        let mapBuckets = numQuantiles |> Option.map BucketChartPreparation.collapse |> defaultArg <| id
        match data with
        | Buckets container ->
            match seriesType with
            | CumValues ->
                // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                container.Buckets
                |> mapBuckets
                |> List.ofArray
                |> List.rev
                |> List.scan
                    (fun (x,y) bucket -> (x + bucket.Weight, y + bucket.Response))
                    (0.0, 0.0)
                |> fun data -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst); Y=(data |> List.map snd) })
            | PredResp ->
                container.Buckets
                |> mapBuckets
                |> List.ofArray
                |> List.map (fun bucket -> (bucket.Sum / bucket.Weight, bucket.Response / bucket.Weight))
                |> fun data -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst); Y = (data |> List.map snd) })
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
                |> fun data -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst) ; Y = (data |> List.map snd) })
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
                |> fun data -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst) ; Y = (data |> List.map snd) })
            | _ -> None
        | SimpleData(data) ->
            match seriesType with
            | Line -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst) ; Y=(data |> List.map snd) })
            | CumulativeLine ->
                // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                data
                |> List.scan
                    (fun (_,ty) (x,y) -> (x, ty + y))
                    (0.0, 0.0)
                |> fun data -> Some (Series.Scatter { Name=name ; X=(data |> List.map fst) ; Y = (data |> List.map snd) })
            | _ -> None
        | TimedData(data) ->
            match seriesType with
            | Line -> Some (Series.TimeScatter { Name=name ; X=(data |> List.map fst) ; Y=(data |> List.map snd) })
            | CumulativeLine ->
                data
                |> List.scan
                    (fun (_,ty) (x,y) -> (x, ty + y))
                    (DateTime.MinValue, 0.0)
                |> List.skip 1
                |> fun data -> Some (Series.TimeScatter { Name=name ; X=(data |> List.map fst) ; Y=(data |> List.map snd) })
            | _ -> None
        | _ -> None

    /// Make a chart from data.
    let chart seriesSpec data : Nemo.Chart =
        let data = data |> TraverseData.filterData seriesSpec.Path |> TraverseData.collapseDivergences
        match data with
        | Grouped m ->
            m
            |> Map.map (fun key -> makeSeries seriesSpec.Config key)
            |> Map.filter (fun _ -> Option.isSome)
            |> Map.map (fun _ -> Option.get)
        | Invalid -> Map.empty
        | _ ->
            match (makeSeries seriesSpec.Config "Data" data) with
            | None -> Map.empty
            | Some series -> Map.ofList [("Data", series)]
        |> Map.toList
        |> List.map snd

