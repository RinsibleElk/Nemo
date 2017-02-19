#load "../packages/FsLab/FsLab.fsx"
#r @"D:\Projects\Nemo\Nemo\bin\Debug\Nemo.dll"

open System
open System.IO
open FsLab
open XPlot.Plotly
open Nemo

let r = Random()
let makeData alpha beta =
    [1..500]
    |> List.map
        (fun i ->
            let x = (float i) / 25000.0
            let y = alpha + x * beta + (r.NextDouble() / 10.0)
            (x,y))
let data() =
    let r = Random()
    [
        "A"
        "B"
    ]
    |> List.map
        (fun a ->
            (a,
                [
                    ("Base", makeData ((r.NextDouble() - 0.5) / 10000.0) (1.0 + ((r.NextDouble() - 0.5)/10.0)))
                    ("Div", makeData ((r.NextDouble() - 0.5) / 10000.0) (1.0 + ((r.NextDouble() - 0.5)/10.0)))
                ]
                |> List.map (fun (name, data) -> (name, (SimpleData data)))
                |> Map.ofList
                |> Grouped))
    |> Map.ofList
    |> Grouped

let spec = FromData([All], Page ([ 1 .. 10 ] |> List.map (fun i -> ((sprintf "Chart %d" i), (SimpleChart(CumulativeLine, None))))))
ChartMaker.saveToFile "Report" (DirectoryInfo @"D:\Nemo\Test") "report.html" ({ Layout = spec }) (data())

