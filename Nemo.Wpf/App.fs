﻿module Nemo.Wpf.MainApp

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open FsXaml

type App = XAML<"App.xaml">

// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main(_) =
    let mainView = MainView()
    let mainViewModel = MainViewModel(mainView)
    let app = App()
    app.Run(mainView)