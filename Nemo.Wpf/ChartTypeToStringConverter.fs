namespace Nemo.Wpf

open Nemo
open System.Windows.Data

[<ValueConversion(typeof<ChartType>, typeof<string>)>]
type ChartTypeToStringConverter() =
    interface IValueConverter
        with
            member __.Convert(value, targetType, _, _) =
                if value |> isNull then failwith ""
                try
                    let chartType = unbox<ChartType> value
                    (box (sprintf "%A" chartType))
                with e ->
                    "" |> box
            member __.ConvertBack(value, targetType, _, _) =
                failwith ""
