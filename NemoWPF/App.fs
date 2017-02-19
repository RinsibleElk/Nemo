module Main

open System
open System.Windows
open ViewModels

let loadXaml<'t> (filename:string) =
    (Uri(filename, UriKind.Relative) |> Application.LoadComponent) :?> 't

[<STAThread>]
[<EntryPoint>]
let main argv =
    let app = "App.xaml" |> loadXaml<Application>
    let mainView = "MainView.xaml" |> loadXaml<Window>
    mainView.DataContext <- MainViewModel()
    app.Run(mainView)
