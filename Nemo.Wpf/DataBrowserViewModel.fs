namespace Nemo.Wpf

open FsXaml
open ViewModule
open ViewModule.FSharp

type DataBrowserView = XAML<"DataBrowserView.xaml">

type DataBrowserViewModel(data) as this =
    inherit ViewModelBase()
    let view = DataBrowserView()
    do view.DataContext <- this
    member __.Data = data |> DataBrowserModelUtils.makeModel |> List.singleton
    member __.View = view

