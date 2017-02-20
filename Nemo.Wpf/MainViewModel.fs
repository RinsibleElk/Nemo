namespace Nemo.Wpf

open FsXaml
open ViewModule
open ViewModule.FSharp

type MainView = XAML<"MainView.xaml">

type MainViewModel() as this =
    inherit ViewModelBase()
    let view = MainView()
    let dataSet data =
        let reportViewModel = ReportViewModel(data)
        view.Content.Content <- reportViewModel.View
    let startViewModel = StartViewModel(dataSet)
    do
        view.Content.Content <- startViewModel.View
        view.DataContext <- this
    member __.View = view
