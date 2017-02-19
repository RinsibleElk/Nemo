namespace ViewModels

open System
open System.IO
open System.Windows
open Newtonsoft.Json
open ViewModule
open ViewModule.FSharp
open Microsoft.Win32
open Nemo

type MainViewModel() as this =
    inherit ViewModelBase()
    let mutable content = box (StartViewModel(fun data -> this.Content <- (box (ReportViewModel(data)))))
    member this.Content
        with get() = content
        and set v =
            if (not (Object.ReferenceEquals(content, v))) then
                content <- v
                this.RaisePropertyChanged <@@ this.Content @@>
