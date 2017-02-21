namespace Nemo.Html

open System
open System.IO
open Nemo

[<RequireQualifiedAccess>]
module ChartWriter =
    /// Make html for a whole html document containing a chart.
    let makeChartPageHtml (chartType:ChartType) (chart:Nemo.Chart) =
        chartType.ChartToHtml chart

    /// Make html for embedding a chart into its own div in an html document.
    let makeChartInlineHtml (chartType:ChartType) (chart:Nemo.Chart) =
        chartType.ChartToInlineHtml chart
