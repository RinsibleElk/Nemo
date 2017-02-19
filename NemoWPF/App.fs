module Main

open System
open System.Windows

let loadXaml<'t> (filename:string) =
    (Uri(filename, UriKind.Relative) |> Application.LoadComponent) :?> 't

[<STAThread>]
[<EntryPoint>]
let main argv =
    ("App.xaml" |> loadXaml<Application>).Run()
