namespace ViewModels

open ViewModule
open ViewModule.FSharp
open Nemo
open System
open System.Collections.ObjectModel

type PageViewModel(data:Data) =
    inherit ViewModelBase()
