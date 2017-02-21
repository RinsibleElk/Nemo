namespace Nemo

open System
open System.IO
open FSharp.Data

type GridLayout = (string * SeriesSpec) list

type PageLayout =
    | Page of GridLayout
    | FromData of DataPath * PageLayout
    | Manual of (string * DataPath * PageLayout) list

type ReportSpec = {
    Layout : PageLayout }

