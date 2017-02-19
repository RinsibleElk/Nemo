namespace ViewModels

open System
open System.IO
open System.Windows
open Newtonsoft.Json
open ViewModule
open ViewModule.FSharp
open Microsoft.Win32
open Nemo

type StartViewModel(callback:Data -> unit) =
    inherit ViewModelBase()
    member __.OpenFileCommand =
        Command.sync
            (fun () ->
                let openFileDialog = new OpenFileDialog()
                let result = openFileDialog.ShowDialog()
                if result.HasValue && result.Value then
                    try
                        let serializer = JsonSerializer()
                        use stream = openFileDialog.OpenFile()
                        use reader = new StreamReader(stream)
                        callback (serializer.Deserialize(reader, typeof<Data>) :?> Data)
                    with e ->
                        ())
