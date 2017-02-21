namespace Nemo.Html

open System
open System.IO
open Nemo

[<RequireQualifiedAccess>]
module ReportWriter =
    let private makeHtmlHeader title allContainerGuids =
        sprintf "<!DOCTYPE html>
<html>
<head>
  <meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\">
  <meta name=\"robots\" content=\"noindex, nofollow\">
  <meta name=\"googlebot\" content=\"noindex, nofollow\">
  <script src=\"https://cdn.plot.ly/plotly-latest.min.js\"></script>
  <script type=\"text/javascript\" src=\"http://code.jquery.com/jquery-1.7.1.js\"></script>
  <script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.16/jquery-ui.js\"></script>
  <link rel=\"stylesheet\" type=\"text/css\" href=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.9/themes/base/jquery-ui.css\">
  <style type=\"text/css\">
*, body, button, input, textarea, select {
  text-rendering: optimizeLegibility;
  -moz-osx-font-smoothing: grayscale;
}

body,div,dl,dt,dd,ul,ol,li,h1,h2,h3,h4,h5,h6,pre,form,fieldset,input,textarea,p,blockquote,th,td {
  margin:0;
  padding:0;
}
table {
  border-collapse:collapse;
  border-spacing:0;
}
fieldset,img {
  border:0;
}
address,caption,cite,code,dfn,em,strong,th,var {
  font-style:normal;
  font-weight:normal;
}
ol,ul {
  list-style:none;
}
caption,th {
  text-align:left;
}
h1,h2,h3,h4,h5,h6 {
  font-size:100%%;
  font-weight:normal;
}
q:before,q:after {
  content:'';
}
abbr,acronym { border:0;}

.ui-widget-header { 
    background: transparent; 
    border: none; 
    border-bottom: 1px solid #c0c0c0; 
    -moz-border-radius: 0px; 
    -webkit-border-radius: 0px; 
    border-radius: 0px; 
} 
.ui-widget-content { 
    background: transparent; 
    border: none; 
    -moz-border-radius: 0px; 
    -webkit-border-radius: 0px; 
    border-radius: 0px; 
} 
.ui-tabs-nav .ui-state-default { 
    background: transparent; 
    border: none; 
} 
.ui-tabs-nav .ui-state-active { 
    background: transparent url(img/uiTabsArrow.png) no-repeat bottom center; 
    border: none; 
} 
.ui-tabs-nav .ui-state-default a { 
    color: #c0c0c0; 
} 
.ui-tabs-nav .ui-state-active a { 
    color: #459e00; 
}
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
    let private makeHtmlGrid chartType m =
        m
        |> List.map (fun (title, chart) -> sprintf "<div><h2>%s</h2>%s</div>" title (ChartWriter.makeChartInlineHtml chartType chart)) |> List.fold (+) "" |> sprintf "<div class=\"griddiv\">%s</div>"
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

    let rec private makeHtml chartType pageLayout data =
        match pageLayout with
        | Page(gridLayout) ->
            ([], gridLayout |> List.map (fun (chartTitle, spec) -> (chartTitle, (ChartMaker.chart spec data))) |> makeHtmlGrid chartType)
        | FromData(path, pageLayout) ->
            TraverseData.tabPaths path data
            |> Map.map
                (fun _ path ->
                    makeHtml chartType pageLayout (TraverseData.filterData path data))
            |> fun m ->
                let containerGuids = m |> Map.toList |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> Map.map (fun _ -> snd) |> Map.toList)
                (newContainerGuid::containerGuids, html)
        | Manual(m) ->
            m
            |> List.map (fun (title, path, pageLayout) -> (title, (makeHtml chartType pageLayout (TraverseData.filterData path data))))
            |> fun m ->
                let containerGuids = m |> List.collect (snd >> fst)
                let (newContainerGuid, html) = makeHtmlTabs (m |> List.map (fun (a,(_,b)) -> (a,b)))
                (newContainerGuid::containerGuids, html)

    /// Get report text.
    let makeReportHtml chartType title reportSpec data =
        let pageLayout = reportSpec.Layout
        makeHtml chartType pageLayout data
        |> fun (containerGuids, bodyHtml) -> (makeHtmlHeader title containerGuids) + bodyHtml + htmlFooter
