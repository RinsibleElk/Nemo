namespace Nemo.Stats

open System
open Nemo

(*
type Bucket = {
    Weight : double
    Min : double
    Median : double
    Max : double
    Mean : double
    Stdev : double
    /// May be NaN if there is no response associated.
    Response : double }
*)
[<RequireQualifiedAccess>]
module Quantiles =
    let private epsilon = 0.0001

    /// Really crappy quantizer. Basically not intended to work, it's just for rapid testing.
    let quantiles values =
        let (nanWeight,nonNanWeight) =
            values
            |> List.fold
                (fun (nanWeight,nonNanWeight) (x,weight) ->
                    if Double.IsNaN weight then (nanWeight,nonNanWeight)
                    else if Double.IsNaN x then (nanWeight + weight,nonNanWeight)
                    else (nanWeight,nonNanWeight + weight))
                (0.0,0.0)
        let sortedData =
            values
            |> List.filter (fst >> Double.IsNaN >> not)
            |> List.filter (snd >> Double.IsNaN >> not)
            |> List.sortBy fst
        let weightPerBucket = nonNanWeight / 1000.0
        let medianWeight = weightPerBucket / 2.0
        // I think I'll just assume I have enough data to make buckets without really worrying about it.
        let rec partition current s =
            match s with
            | [] -> Seq.empty
            | (x, weight)::s ->
                let weightSoFarInBucket = current.Weight
                let median = if weightSoFarInBucket > medianWeight then current.Median else x
                let proposedWeight = weightSoFarInBucket + weight
                let (current, emit) =
                    if proposedWeight > weightPerBucket - epsilon then
                        let currentWeight = max 0.0 (proposedWeight - weightPerBucket)
                        let current = {
                            Weight = currentWeight
                            Min = x
                            Median = x
                            Max = x
                            Response = Double.NaN
                            Sum = x * currentWeight
                            SumSquares = x * x * currentWeight }
                        let emit =
                            let emitWeight = min proposedWeight weightPerBucket
                            {
                                Weight = emitWeight
                                Min = current.Min
                                Median = median
                                Max = current.Max
                                Sum = current.Sum + (x * emitWeight)
                                SumSquares = current.SumSquares + (x * x * emitWeight)
                                Response = Double.NaN
                            }
                        (current, Some emit)
                    else
                        let current = {
                            Weight = proposedWeight
                            Min = current.Min
                            Median = median
                            Max = x
                            Response = Double.NaN
                            Sum = current.Sum + (x * proposedWeight)
                            SumSquares = current.SumSquares + (x * x * proposedWeight) }
                        (current, None)
                seq {
                    if emit.IsSome then yield emit.Value
                    yield! (partition current s) }
        match (sortedData |> List.ofSeq) with
        | (x,weight)::t ->
            let current = {
                Weight = weight
                Min = x
                Median = x
                Max = x
                Response = Double.NaN
                Sum = x * weight
                SumSquares = x * x * weight }
            {   NanWeight = nanWeight
                NonNanWeight = nonNanWeight
                Buckets = (t |> partition current |> Seq.toArray) }
        | [] -> failwith ""
