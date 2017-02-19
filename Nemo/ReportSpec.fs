namespace Nemo

open System
open System.IO
open XPlot.Plotly
open FSharp.Data

type BucketChartType =
    | CumValues
    | Cdf
    | Pdf

type BucketChartOptions = {
    Foo : string }

type SimpleChartType =
    | Line
    | CumulativeLine

type SimpleChartOptions = {
    Bar : string }

type ChartSpec =
    | BucketChart of BucketChartType * BucketChartOptions option
    | SimpleChart of SimpleChartType * SimpleChartOptions option

type GridLayout = (string * ChartSpec) list

type DataNode =
    | Specific of string
    | All

type DataPath = DataNode list

type PageLayout =
    | Page of GridLayout
    | FromData of DataPath * PageLayout
    | Manual of Map<string, DataPath * PageLayout>

type ReportSpec = {
    Layout : PageLayout }

[<RequireQualifiedAccess>]
module TraverseData =
    /// Filter data by a DataPath.
    let rec filterData (dataPath:DataPath) data =
        match (dataPath, data) with
        | ([], _) -> data
        | (Specific s::remainingPath, Grouped m) ->
            m
            |> Map.tryFind s
            |> Option.map (fun data -> filterData remainingPath data)
            |> defaultArg <| Invalid
        | (All::remainingPath, Grouped m) ->
            m
            |> Map.map (fun _ data -> filterData remainingPath data)
            |> Grouped
        | _ -> Invalid

    /// Given a data path and some data, figure out what tabs will be required and make specific filter paths to replace them.
    let rec tabPaths (dataPath:DataPath) data : Map<string, DataPath> =
        match (dataPath, data) with
        | ([], _) -> List.singleton ("", dataPath) |> Map.ofList
        | (Specific s::remainingPath, Grouped m) ->
            m
            |> Map.tryFind s
            |> Option.map (fun data -> tabPaths remainingPath data)
            |> defaultArg <| Map.empty
        | (All::remainingPath, Grouped m) ->
            m
            |> Map.map
                (fun key data ->
                    let paths = tabPaths remainingPath data
                    paths
                    |> Map.toList
                    |> List.map
                        (fun (innerKey, innerPath) ->
                            ((if innerKey = "" then key else key + " - " + innerKey), (Specific key)::innerPath)))
            |> Map.toList
            |> List.collect snd
            |> Map.ofList
        | _ -> Map.empty

    /// Build divergences from data.
    let rec collapseDivergences (data:Data) : Data =
        match data with
        | Grouped m ->
            m
            |> Map.toList
            |> List.collect
                (fun (key,data) ->
                    match collapseDivergences data with
                    | Grouped m2 ->
                        m2
                        |> Map.toList
                        |> List.map (fun (key2, data2) -> (key + " - " + key2, data2))
                    | Invalid ->
                        []
                    | d ->
                        [(key, d)])
            |> function | [] -> Invalid | li -> li |> Map.ofList |> Grouped
        | _ -> data

[<RequireQualifiedAccess>]
module ChartMaker =
    /// Make a trace from some data.
    let makeSeries chartSpec name data : Trace option =
        match chartSpec with
        | BucketChart (ty,b) ->
            match data with
            | Buckets container ->
                match ty with
                | CumValues ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    container.Buckets
                    |> List.ofArray
                    |> List.rev
                    |> List.scan
                        (fun (x,y) bucket -> (x + bucket.Weight, y + bucket.Weight * bucket.Response))
                        (0.0, 0.0)
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Cdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    container.Buckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / 1000.0) + 0.0005)))
                    |> List.ofArray
                    |> fun l -> (minValue::l)@[maxValue]
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | Pdf ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    let minValue = (container.Buckets.[0].Min, 0.0)
                    let maxValue = (container.Buckets.[999].Max, 1.0)
                    container.Buckets
                    |> Array.mapi
                        (fun i bucket ->
                            (bucket.Median, (((float i) / 1000.0) + 0.0005)))
                    |> List.ofArray
                    |> fun l -> l@[maxValue]
                    |> List.scan
                        (fun (_, (lastX, lastY)) (x, y) ->
                            let g = (y - lastY) / (x - lastX)
                            ((x, g), (x, y)))
                        (minValue, minValue)
                    |> List.map fst
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
            | _ -> None
        | SimpleChart (ty,b) ->
            match ty with
            | Line ->
                match data with
                | SimpleData(data) ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None
            | CumulativeLine ->
                match data with
                | SimpleData(data) ->
                    // We make use of the horrible List.scan behaviour where it inserts an extra element at the beginning.
                    data
                    |> List.scan
                        (fun (_,ty) (x,y) -> (x, ty + y))
                        (0.0, 0.0)
                    |> fun data -> Some (Scatter(name=name, x=(data |> List.map fst), y = (data |> List.map snd)) :> Trace)
                | _ -> None

    /// Make a chart from data.
    let chart chartSpec data =
        let data = TraverseData.collapseDivergences data
        match data with
        | Grouped m ->
            m
            |> Map.map (fun key -> makeSeries chartSpec key)
            |> Map.filter (fun _ -> Option.isSome)
            |> Map.map (fun _ -> Option.get)
        | Invalid -> Map.empty
        | _ ->
            match (makeSeries chartSpec "Data" data) with
            | None -> Map.empty
            | Some trace -> Map.ofList [("Data", trace)]
        |> Map.toList
        |> List.map snd
        |> Chart.Plot

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
        |> Map.toList
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
            ([], gridLayout |> List.map (fun (chartTitle, spec) -> (chartTitle, (chart spec data))) |> makeHtmlGrid)
        | FromData(path, pageLayout) ->
            TraverseData.tabPaths path data
            |> Map.map
                (fun _ path ->
                    makeHtml pageLayout (TraverseData.filterData path data))
            |> fun m ->
                let containerGuids = m |> Map.toList |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> Map.map (fun _ -> snd))
                (newContainerGuid::containerGuids, html)
        | Manual(m) ->
            m
            |> Map.map (fun _ (path, pageLayout) -> makeHtml pageLayout (TraverseData.filterData path data))
            |> fun m ->
                let containerGuids = m |> Map.toList |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> Map.map (fun _ -> snd))
                (newContainerGuid::containerGuids, html)

    /// Save a report to file from a report spec.
    let saveToFile title (dir:DirectoryInfo) filename reportSpec data =
        let pageLayout = reportSpec.Layout
        makeHtml pageLayout data
        |> fun (containerGuids, bodyHtml) ->
            let html = (makeHtmlHeader title containerGuids) + bodyHtml + htmlFooter
            File.WriteAllText(Path.Combine(dir.FullName, filename), html)
