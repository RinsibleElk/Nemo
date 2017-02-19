namespace Nemo.Wpf

open FsXaml
open ViewModule
open ViewModule.FSharp

type MainView = XAML<"MainView.xaml">

type MainViewModel(view:MainView) =
    inherit ViewModelBase()
    let dataSet data =
        let reportView = ReportView()
        let reportViewModel = ReportViewModel(reportView, data)
        reportView.DataContext <- reportViewModel
        view.Content.Content <- reportView
    let startView = StartView()
    let startViewModel = StartViewModel(startView, dataSet)
    do
        startView.DataContext <- startViewModel
        view.Content.Content <- startView
