namespace Nemo

open System

type ScatterSeries = {
    Name    : string
    X       : double list
    Y       : double list }

type TimeScatterSeries = {
    Name    : string
    X       : DateTime list
    Y       : double list }

type Series =
    | Scatter of ScatterSeries
    | TimeScatter of TimeScatterSeries

type Chart = Series list
