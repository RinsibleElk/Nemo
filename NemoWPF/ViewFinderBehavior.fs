namespace Behaviors

open System
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open System.Windows.Interactivity

type ViewFinderBehavior() =
    inherit Behavior<ContentControl>()
    // pretty noddy
    let loadUserControl o (filename:string) =
        let userControl = (Uri(filename, UriKind.Relative) |> Application.LoadComponent) :?> UserControl
        userControl.DataContext <- o
        userControl
    let mutable context : INotifyPropertyChanged = null
    let mutable name : string = null
    let mutable contentControl : ContentControl = null
    let set() =
        if context <> null then
            let contextTy = context.GetType()
            let property = contextTy.GetProperty(name)
            let o = property.GetMethod.Invoke(context, [||])
            let xamlName = o.GetType().Name.Replace("Model", ".xaml")
            contentControl.Content <- loadUserControl o xamlName
    let propertyChanged (_:PropertyChangedEventArgs) = set()
    let loaded (_:RoutedEventArgs) = set()
    let setContext newContext =
        if newContext <> null then
            context <- newContext
            context.PropertyChanged |> Event.add propertyChanged
    override this.OnAttached() =
        let parent = Application.Current.MainWindow
        name <- this.AssociatedObject.Name
        contentControl <- this.AssociatedObject
        contentControl.Loaded |> Event.add loaded
        contentControl.DataContextChanged |> Event.add (fun c -> setContext (unbox<INotifyPropertyChanged>(c.NewValue)))
        setContext (unbox<INotifyPropertyChanged>(contentControl.DataContext))
