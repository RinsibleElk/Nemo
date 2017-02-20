namespace Nemo.Wpf

open Nemo
open System
open System.Collections.ObjectModel

// For the moment I don't worry about eagerly expanding this data out.

[<AbstractClass>]
type DataBrowserModel() =
    abstract member Children : DataBrowserModel list with get
    abstract member Name : string with get

[<Sealed>]
type SimpleDataPointDataBrowserModel(name, value:string) =
    inherit DataBrowserModel()
    override __.Children = []
    override __.Name = name
    member __.Value = value

[<Sealed>]
type BucketDataPointDataBrowserModel(i, bucket) =
    inherit DataBrowserModel()
    override __.Children = []
    override __.Name = sprintf "%d" i
    member __.Weight = bucket.Weight
    member __.Min = bucket.Min
    member __.Median = bucket.Median
    member __.Max = bucket.Max
    member __.Sum = bucket.Sum
    member __.SumSquares = bucket.SumSquares
    member __.Response = bucket.Response

[<Sealed>]
type TimedDataDataBrowserModel(name, data:(DateTime * double) list) =
    inherit DataBrowserModel()
    override __.Children = data |> List.map (fun (time, value) -> SimpleDataPointDataBrowserModel(time.ToString("o"), value.ToString()) :> DataBrowserModel)
    override __.Name = sprintf "%s (Timed Data)" name

[<Sealed>]
type SimpleDataDataBrowserModel(name, data:(double * double) list) =
    inherit DataBrowserModel()
    override __.Children = data |> List.map (fun (x, y) -> SimpleDataPointDataBrowserModel(x.ToString(), y.ToString()) :> DataBrowserModel)
    override __.Name = sprintf "%s (XY Data)" name

[<Sealed>]
type InvalidDataBrowserModel(name) =
    inherit DataBrowserModel()
    override __.Children = []
    override __.Name = name

[<Sealed>]
type BucketsDataBrowserModel(name, container) =
    inherit DataBrowserModel()
    override __.Children = container.Buckets |> List.ofArray |> List.mapi (fun i bucket -> BucketDataPointDataBrowserModel(i, bucket) :> DataBrowserModel)
    override __.Name = sprintf "%s (Buckets)" name

[<Sealed>]
type GroupedDataBrowserModel(name, m:Map<string, Data>) =
    inherit DataBrowserModel()
    override __.Children =
        m
        |> Map.toList
        |> List.map
            (fun (n, data) ->
                match data with
                | Grouped m2 -> GroupedDataBrowserModel(n, m2) :> DataBrowserModel
                | TimedData l -> TimedDataDataBrowserModel(n, l) :> DataBrowserModel
                | SimpleData l -> SimpleDataDataBrowserModel(n, l) :> DataBrowserModel
                | Buckets container -> BucketsDataBrowserModel(n, container) :> DataBrowserModel
                | Invalid -> InvalidDataBrowserModel(n) :> DataBrowserModel)
    override __.Name = name

[<RequireQualifiedAccess>]
module DataBrowserModelUtils =
    let makeModel data =
        let level = -4
        let n = "Root"
        match data with
        | Grouped m2 -> GroupedDataBrowserModel(n, m2) :> DataBrowserModel
        | TimedData l -> TimedDataDataBrowserModel(n, l) :> DataBrowserModel
        | SimpleData l -> SimpleDataDataBrowserModel(n, l) :> DataBrowserModel
        | Buckets container -> BucketsDataBrowserModel(n, container) :> DataBrowserModel
        | Invalid -> InvalidDataBrowserModel(n) :> DataBrowserModel
