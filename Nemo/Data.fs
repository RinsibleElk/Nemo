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
