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
    let mutable activated = false
    app.Activated
    |> Event.add
        (fun _ ->
            if (not activated) then
                let appStyle = ThemeManager.DetectAppStyle(Application.Current)
                ThemeManager.ChangeAppStyle(Application.Current,
                                            ThemeManager.GetAccent("Blue"),
                                            ThemeManager.GetAppTheme("BaseLight"));
                mainViewModel.OnActivated()
                activated <- true)
    app.Run(mainViewModel.View)
