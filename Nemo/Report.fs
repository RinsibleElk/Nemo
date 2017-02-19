﻿namespace Nemo

open System
open System.IO
open XPlot.Plotly
open FSharp.Data

type GridLayout = (string * ChartSpec) list

type PageLayout =
    | Page of GridLayout
    | FromData of DataPath * PageLayout
    | Manual of (string * DataPath * PageLayout) list

type ReportSpec = {
    Layout : PageLayout }

[<RequireQualifiedAccess>]
module Report =
    let private makeHtmlHeader title allContainerGuids =
        sprintf "<!DOCTYPE html>
<html>
<head>
  <meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\">
  <meta name=\"robots\" content=\"noindex, nofollow\">
  <meta name=\"googlebot\" content=\"noindex, nofollow\">
  <script src=\"https://cdn.plot.ly/plotly-latest.min.js\"></script>
  <script type=\"text/javascript\" src=\"http://code.jquery.com/jquery-1.7.1.js\"></script>
  <link rel=\"stylesheet\" type=\"text/css\" href=\"css/normalize.css\">
  <script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.16/jquery-ui.js\"></script>
  <link rel=\"stylesheet\" type=\"text/css\" href=\"css/result-light.css\">
  <link rel=\"stylesheet\" type=\"text/css\" href=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.9/themes/base/jquery-ui.css\">
  <style type=\"text/css\">
  </style>
  <title>%s</title>
<script type='text/javascript'>//<![CDATA[
$(window).load(function(){
%s
});//]]> 
</script>
        <style>
            div.griddiv {
                display: grid;
                grid-template-columns: auto auto;
                grid-gap: 10px;
            }
        </style>
    </head>
    <body>" title (allContainerGuids |> List.map (sprintf "$('#%s').tabs();\n") |> List.fold (+) "")
    let private htmlFooter = "
    </body>
</html>"
    let private getChartHtml (chart:PlotlyChart) =
        "
        " + chart.GetInlineHtml()
    let private makeHtmlGrid m =
        m
        |> List.map (fun (title, chart) -> sprintf "<div><h2>%s</h2>%s</div>" title (chart |> getChartHtml)) |> List.fold (+) "" |> sprintf "<div class=\"griddiv\">%s</div>"
    let private makeHtmlTabs m =
        let containerGuid = Guid.NewGuid().ToString()
        m
        |> List.map
            (fun (tabText, html) ->
                let contentGuid = Guid.NewGuid().ToString()
                (contentGuid, (tabText, html)))
        |> fun li ->
            let makeTabButtonHtml (text,i) (contentGuid, (tabText, html)) =
                if i = 0 then
                    ((text + (sprintf "<li class=\"active\"><a href=\"#%s\">%s</a></li>" contentGuid tabText)), 1)
                else
                    ((text + (sprintf "<li><a href=\"#%s\">%s</a></li>" contentGuid tabText)), i + 1)
            let makeTabContentHtml (text,i) (contentGuid, (tabText, html)) =
                if i = 0 then
                    (text + (sprintf "<div id=\"%s\" class=\"tab-pane fade in active\">%s</div>" contentGuid html), 1)
                else
                    (text + (sprintf "<div id=\"%s\" class=\"tab-pane fade\">%s</div>" contentGuid html), i + 11)
            let tabControl =
                li
                |> List.fold makeTabButtonHtml ("<ul class=\"nav nav-tabs\">", 0)
                |> fst
                |> sprintf "%s</ul>"
            let html =
                li
                |> List.fold makeTabContentHtml ((sprintf "" ), 0)
                |> fst
                |> sprintf "<div id=\"%s\" class=\"tab-content\">%s%s</div>" containerGuid tabControl
            (containerGuid, html)

    let rec private makeHtml pageLayout data =
        match pageLayout with
        | Page(gridLayout) ->
            ([], gridLayout |> List.map (fun (chartTitle, spec) -> (chartTitle, (ChartMaker.chart spec data))) |> makeHtmlGrid)
        | FromData(path, pageLayout) ->
            TraverseData.tabPaths path data
            |> Map.map
                (fun _ path ->
                    makeHtml pageLayout (TraverseData.filterData path data))
            |> fun m ->
                let containerGuids = m |> Map.toList |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> Map.map (fun _ -> snd) |> Map.toList)
                (newContainerGuid::containerGuids, html)
        | Manual(m) ->
            m
            |> List.map (fun (title, path, pageLayout) -> (title, (makeHtml pageLayout (TraverseData.filterData path data))))
            |> fun m ->
                let containerGuids = m |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> List.map (fun (a,(_,b)) -> (a,b)))
                (newContainerGuid::containerGuids, html)

    /// Save a report to file from a report spec.
    let saveToFile title (dir:DirectoryInfo) filename reportSpec data =
        let pageLayout = reportSpec.Layout
        makeHtml pageLayout data
        |> fun (containerGuids, bodyHtml) ->
            let html = (makeHtmlHeader title containerGuids) + bodyHtml + htmlFooter
            File.WriteAllText(Path.Combine(dir.FullName, filename), html)