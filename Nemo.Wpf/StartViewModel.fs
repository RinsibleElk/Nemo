namespace Nemo.Wpf

open ViewModule
open ViewModule.FSharp
open Newtonsoft.Json
open System.IO
open System.Windows.Controls
open Microsoft.Win32
open Nemo
open FsXaml

type StartView = XAML<"StartView.xaml">

type StartViewModel(view:StartView, callback) =
    inherit ViewModelBase()
    let deserialize =
        let serializer = JsonSerializer()
        fun (reader:TextReader) -> serializer.Deserialize(reader, typeof<Data>) |> unbox<Data>
    let openData =
        Command.sync
            (fun () ->
                let openFileDialog = OpenFileDialog()
                let result = openFileDialog.ShowDialog()
                if result.HasValue && result.Value then
                    try
                        use stream = openFileDialog.OpenFile()
                        use reader = new StreamReader(stream)
                        reader |> deserialize |> callback
                    with e ->
                        ())
    member __.OpenData = openData
