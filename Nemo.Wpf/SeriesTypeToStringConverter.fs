namespace Nemo.Wpf

open Nemo
open System.Windows.Data

[<ValueConversion(typeof<SeriesType>, typeof<string>)>]
type SeriesTypeToStringConverter() =
    interface IValueConverter
        with
            member __.Convert(value, targetType, _, _) =
                if value |> isNull then failwith ""
                try
                    let seriesType = unbox<SeriesType> value
                    (box (sprintf "%A" seriesType))
                with e ->
                    "" |> box
            member __.ConvertBack(value, targetType, _, _) =
                failwith ""
