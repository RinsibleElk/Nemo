namespace Nemo.Html

open Nemo

type ChartType =
    | Plotly
    | Google
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
                failwith ""

