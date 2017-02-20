namespace Nemo.Wpf

open Nemo
open System
open System.Collections.ObjectModel

// For the moment I don't worry about eagerly expanding this data out.

[<AbstractClass>]
type DataBrowserModel() =
    abstract member Children : DataBrowserModel list with get
    abstract member Name : string with get
    abstract member Value : string with get
    abstract member Level : int with get

[<Sealed>]
type SimpleDataPointDataBrowserModel(level, name, value) =
    inherit DataBrowserModel()
    override __.Children = []
    override __.Name = name
    override __.Value = value
    override __.Level = level

[<Sealed>]
type TimedDataDataBrowserModel(level, name, data:(DateTime * double) list) =
    inherit DataBrowserModel()
    override __.Children = data |> List.map (fun (time, value) -> SimpleDataPointDataBrowserModel(level + 4, time.ToString("o"), value.ToString()) :> DataBrowserModel)
    override __.Name = name
    override __.Value = "TimedData"
    override __.Level = level

[<Sealed>]
type SimpleDataDataBrowserModel(level, name, data:(double * double) list) =
    inherit DataBrowserModel()
    override __.Children = data |> List.map (fun (x, y) -> SimpleDataPointDataBrowserModel(level + 4, x.ToString(), y.ToString()) :> DataBrowserModel)
    override __.Name = name
    override __.Value = "SimpleData"
    override __.Level = level

[<Sealed>]
type InvalidDataBrowserModel(level, name) =
    inherit DataBrowserModel()
    override __.Children = []
    override __.Name = name
    override __.Value = "Invalid"
    override __.Level = level

[<Sealed>]
type BucketsDataBrowserModel(level, name, container) =
    inherit DataBrowserModel()
    override __.Children = container.Buckets |> List.ofArray |> List.map (fun bucket -> SimpleDataPointDataBrowserModel(level + 4, bucket.Median.ToString(), bucket.ToString()) :> DataBrowserModel)
    override __.Name = name
    override __.Value = "Buckets"
    override __.Level = level

[<Sealed>]
type GroupedDataBrowserModel (level, name, m:Map<string, Data>) =
    inherit DataBrowserModel()
    override __.Children =
        m
        |> Map.toList
        |> List.map
            (fun (n, data) ->
                match data with
                | Grouped m2 -> GroupedDataBrowserModel(level + 4, n, m2) :> DataBrowserModel
                | TimedData l -> TimedDataDataBrowserModel(level + 4, n, l) :> DataBrowserModel
                | SimpleData l -> SimpleDataDataBrowserModel(level + 4, n, l) :> DataBrowserModel
                | Buckets container -> BucketsDataBrowserModel(level + 4, n, container) :> DataBrowserModel
                | Invalid -> InvalidDataBrowserModel(level + 4, n) :> DataBrowserModel)
    override __.Name = name
    override __.Value = "Grouped"
    override __.Level = level

[<RequireQualifiedAccess>]
module DataBrowserModelUtils =
    let makeModel data =
        let level = -4
        let n = "Root"
        match data with
        | Grouped m2 -> GroupedDataBrowserModel(level + 4, n, m2) :> DataBrowserModel
        | TimedData l -> TimedDataDataBrowserModel(level + 4, n, l) :> DataBrowserModel
        | SimpleData l -> SimpleDataDataBrowserModel(level + 4, n, l) :> DataBrowserModel
        | Buckets container -> BucketsDataBrowserModel(level + 4, n, container) :> DataBrowserModel
        | Invalid -> InvalidDataBrowserModel(level + 4, n) :> DataBrowserModel
