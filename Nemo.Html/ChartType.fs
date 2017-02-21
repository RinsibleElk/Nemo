namespace Nemo.Html

open Nemo

type ChartType =
    | Plotly
    | Google
    | FSharp
    with
        member this.ChartToHtml =
            match this with
            | Plotly ->
                fun (chart:Nemo.Chart) ->
                    chart
                    |> List.map
                        (fun series ->
                            match series with
                            | Series.Scatter(s) -> XPlot.Plotly.Graph.Scatter(name=s.Name, x=s.X, y=s.Y) :> XPlot.Plotly.Graph.Trace
                            | Series.TimeScatter(s) -> XPlot.Plotly.Graph.Scatter(name=s.Name, x=s.X, y=s.Y) :> XPlot.Plotly.Graph.Trace)
                    |> XPlot.Plotly.Chart.Plot
                    |> fun c -> c.GetHtml()
            | Google ->
                fun (chart:Nemo.Chart) ->
                    chart
                    |> List.head
                    |> function
                        | Series.Scatter s ->
                            let chart = chart |> List.map (function | Series.Scatter s -> s | _ -> failwith "Inconsistent series")
                            let plot = XPlot.GoogleCharts.Chart.Combo(chart |> Seq.map (fun series -> Seq.zip series.X series.Y))
                            plot.WithLabels(chart |> Seq.map (fun series -> series.Name))
                            plot.GetHtml()
                        | Series.TimeScatter s ->
                            let chart = chart |> List.map (function | Series.TimeScatter s -> s | _ -> failwith "Inconsistent series")
                            let plot = XPlot.GoogleCharts.Chart.Combo(chart |> Seq.map (fun series -> Seq.zip series.X series.Y))
                            plot.WithLabels(chart |> Seq.map (fun series -> series.Name))
                            plot.GetHtml()
            | FSharp ->
                failwith ""

        member this.ChartToInlineHtml =
            match this with
            | Plotly ->
                fun (chart:Nemo.Chart) ->
                    chart
                    |> List.map
                        (fun series ->
                            match series with
                            | Series.Scatter(s) -> XPlot.Plotly.Graph.Scatter(name=s.Name, x=s.X, y=s.Y) :> XPlot.Plotly.Graph.Trace
                            | Series.TimeScatter(s) -> XPlot.Plotly.Graph.Scatter(name=s.Name, x=s.X, y=s.Y) :> XPlot.Plotly.Graph.Trace)
                    |> XPlot.Plotly.Chart.Plot
                    |> fun c -> c.GetInlineHtml()
            | Google ->
                fun (chart:Nemo.Chart) ->
                    chart
                    |> List.head
                    |> function
                        | Series.Scatter s ->
                            let chart = chart |> List.map (function | Series.Scatter s -> s | _ -> failwith "Inconsistent series")
                            let plot = XPlot.GoogleCharts.Chart.Combo(chart |> Seq.map (fun series -> Seq.zip series.X series.Y))
                            plot.WithLabels(chart |> Seq.map (fun series -> series.Name))
                            plot.GetInlineHtml()
                        | Series.TimeScatter s ->
                            let chart = chart |> List.map (function | Series.TimeScatter s -> s | _ -> failwith "Inconsistent series")
                            let plot = XPlot.GoogleCharts.Chart.Combo(chart |> Seq.map (fun series -> Seq.zip series.X series.Y))
                            plot.WithLabels(chart |> Seq.map (fun series -> series.Name))
                            plot.GetInlineHtml()
            | FSharp ->
                fun (chart:Nemo.Chart) ->
                    chart
                    |> List.map
                        (fun series ->
                            match series with
                            | Series.Scatter(s) -> FSharp.Charting.Chart.Line(Seq.zip s.X s.Y, s.Name)
                            | Series.TimeScatter(s) -> FSharp.Charting.Chart.Line(Seq.zip s.X s.Y, s.Name))
                    |> FSharp.Charting.Chart.Combine
                    |> fun c ->
                        use ms = new System.IO.MemoryStream()
                        let bmp = c.CopyAsBitmap()
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Gif)
                        sprintf @"<img style='display:block; width:%dpx;height:%dpx;' src='data:image/gif;base64,%s' />" bmp.Width bmp.Height (System.Convert.ToBase64String(ms.ToArray()))

