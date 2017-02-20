module Nemo.Wpf.MainApp

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open FsXaml
open MahApps.Metro

type App = XAML<"App.xaml">

// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main(_) =
    let mainViewModel = MainViewModel()
    let app = App()
    app.Activated
    |> Event.add
        (fun _ ->
            let appStyle = ThemeManager.DetectAppStyle(Application.Current)
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent("Blue"),
                                        ThemeManager.GetAppTheme("BaseLight"));
            ())
    app.Run(mainViewModel.View)
