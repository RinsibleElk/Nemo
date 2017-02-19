namespace Nemo.Wpf

open Nemo
open System.Windows.Data

[<ValueConversion(typeof<Choice<string, DataNode>>, typeof<string>)>]
type DataNodeToStringConverter() =
    interface IValueConverter
        with
            member __.Convert(value, targetType, _, _) =
                if value |> isNull then failwith ""
                try
                    let dataNode = unbox<Choice<string, DataNode>> value
                    match dataNode with
                    | Choice1Of2 s -> s |> box
                    | Choice2Of2 dataNode ->
                        match dataNode with
                        | All -> "(All)" |> box
                        | Specific s -> s |> box
                with e ->
                    "" |> box
            member __.ConvertBack(value, targetType, _, _) =
                failwith ""
