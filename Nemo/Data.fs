namespace Nemo

open System

type Bucket = {
    Weight : double
    Min : double
    Median : double
    Max : double
    Sum : double
    SumSquares : double
    /// May be NaN if there is no response associated.
    Response : double }
    with
        override this.ToString() =
            sprintf "{ Weight=%f; Min=%f; Median=%f; Max=%f; Sum=%f; SumSquares=%f; Response=%f }" this.Weight this.Min this.Median this.Max this.Sum this.SumSquares this.Response

type BucketContainer = {
    NanWeight : double
    NonNanWeight : double
    /// There are always 1000 of these.
    Buckets : Bucket[] }

type Data =
    | Buckets of BucketContainer
    | Grouped of Map<string, Data>
    | TimedData of (DateTime * double) list
    | SimpleData of (double * double) list
    | Invalid
    with
        /// Is a series type valid for this data?
        member this.IsSeriesTypeValid(seriesType) =
            match (this, seriesType) with
            | (Invalid, _) -> false
            | (SimpleData _, Line)
            | (SimpleData _, CumulativeLine)
            | (TimedData _, Line)
            | (TimedData _, CumulativeLine) -> true
            | (Buckets _, CumValues)
            | (Buckets _, PredResp)
            | (Buckets _, Cdf)
            | (Buckets _, Pdf) -> true
            | (Grouped m, _) -> (m |> Map.toList |> List.map (snd >> fun d -> d.IsSeriesTypeValid(seriesType)) |> List.fold (fun o a -> if o |> Option.isNone then (Some a) else (Some (o.Value && a))) None) |> fun o -> o.IsSome && o.Value
            | _ -> false
