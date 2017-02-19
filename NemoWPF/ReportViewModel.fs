namespace ViewModels

open ViewModule
open ViewModule.FSharp
open Nemo
open System
open System.Collections.ObjectModel

type ReportViewModel(data:Data) =
    inherit ViewModelBase()
    let groups =
        match data with
        | Grouped m -> ObservableCollection<string>(m |> Map.toList |> List.map fst)
        | _ -> ObservableCollection<string>()
    let mutable page = PageViewModel(data)
    member __.Groups with get() = groups
    member this.Page
        with get() = page
        and set v =
            if (not (Object.ReferenceEquals(page, v))) then
                page <- v
                this.RaisePropertyChanged <@@ this.Page @@>


