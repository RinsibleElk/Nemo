namespace Nemo.Wpf

open ViewModule
open ViewModule.FSharp
open Newtonsoft.Json
open System.IO
open System.Windows.Controls
open Microsoft.Win32
open Nemo
open FsXaml
open System
open System.Collections.ObjectModel

type ReportView = XAML<"ReportView.xaml">

type ReportViewModel(view:ReportView, data) as this =
    inherit ViewModelBase()
    let mutable currentData = data
    let f =
        match currentData with
        | Grouped m ->
            if m |> Map.isEmpty then [Choice1Of2 ""]
            else
                (All::(m |> Map.toList |> List.map (fst >> Specific))) |> List.map Choice2Of2
         | _ -> [Choice1Of2 ""]
    let mutable filters = ObservableCollection<Choice<string,DataNode>>(f)
    let mutable currentFilters : DataPath = []
    let mutable selectedFilter = f |> List.head
    let mutable selectedChartType = ChartType.CumValues
    let mutable addFilterCanExecute = true
    let addFilter =
        Command.syncChecked
            (fun () ->
                match selectedFilter with
                | Choice2Of2 sf ->
                    match currentData with
                    | Grouped m ->
                        currentFilters <- currentFilters @ [sf]
                        this.RaisePropertyChanged <@@ this.CurrentFilters @@>
                        currentData <- if m |> Map.isEmpty then Invalid else m |> Map.toList |> List.head |> snd
                        let f =
                            match currentData with
                            | Grouped m ->
                                if m |> Map.isEmpty then [Choice1Of2 ""]
                                else
                                    (All::(m |> Map.toList |> List.map (fst >> Specific))) |> List.map Choice2Of2
                             | _ -> [Choice1Of2 ""]
                        this.Filters <- ObservableCollection<Choice<string,DataNode>>(f)
                        this.SelectedFilter <- f |> List.head
                    | _ ->
                        ()
                | _ -> ())
            (fun () -> addFilterCanExecute)
    let mutable removeFiltersCanExecute = true
    let removeFilters =
        Command.syncChecked
            (fun () ->
                currentData <- data
                let f =
                    match currentData with
                    | Grouped m ->
                        if m |> Map.isEmpty then [Choice1Of2 ""]
                        else
                            (All::(m |> Map.toList |> List.map (fst >> Specific))) |> List.map Choice2Of2
                     | _ -> [Choice1Of2 ""]
                this.Filters <- ObservableCollection<Choice<string,DataNode>>(f)
                currentFilters <- []
                this.RaisePropertyChanged <@@ this.CurrentFilters @@>
                this.SelectedFilter <- f |> List.head
                ())
            (fun () -> removeFiltersCanExecute)
    let addChart =
        Command.sync
            (fun () ->
                removeFiltersCanExecute <- false
                removeFilters.RaiseCanExecuteChanged()
                addFilterCanExecute <- false
                addFilter.RaiseCanExecuteChanged()
                ())
    member this.Filters
        with get() = filters
        and set v =
            if (not (Object.ReferenceEquals(filters, v))) then
                filters <- v
                this.RaisePropertyChanged <@@ this.Filters @@>
    member this.SelectedFilter
        with get() = selectedFilter
        and set v =
            if (not (Object.ReferenceEquals(selectedFilter, v))) then
                selectedFilter <- v
                this.RaisePropertyChanged <@@ this.SelectedFilter @@>
    member this.CurrentFilters
        with get() =
            if currentFilters |> List.isEmpty then "No filters selected."
            else currentFilters |> List.map (function | All -> "(All)" | Specific s -> s) |> List.fold (fun a b -> if a = "" then b else a + ";" + b) ""
    member __.AddFilter = addFilter
    member __.RemoveFilters = removeFilters
    member this.SelectedChartType
        with get() = selectedChartType
        and set v =
            selectedChartType <- v
            this.RaisePropertyChanged <@@ this.SelectedChartType @@>
    member __.AddChart = addChart