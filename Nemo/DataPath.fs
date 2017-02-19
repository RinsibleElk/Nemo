namespace Nemo

type DataNode =
    | Specific of string
    | All

type DataPath = DataNode list

[<RequireQualifiedAccess>]
module TraverseData =
    /// Filter data by a DataPath.
    let rec filterData (dataPath:DataPath) data =
        match (dataPath, data) with
        | ([], _) -> data
        | (Specific s::remainingPath, Grouped m) ->
            m
            |> Map.tryFind s
            |> Option.map (fun data -> filterData remainingPath data)
            |> defaultArg <| Invalid
        | (All::remainingPath, Grouped m) ->
            m
            |> Map.map (fun _ data -> filterData remainingPath data)
            |> Grouped
        | _ -> Invalid

    /// Given a data path and some data, figure out what tabs will be required and make specific filter paths to replace them.
    let rec tabPaths (dataPath:DataPath) data : Map<string, DataPath> =
        match (dataPath, data) with
        | ([], _) -> List.singleton ("", dataPath) |> Map.ofList
        | (Specific s::remainingPath, Grouped m) ->
            m
            |> Map.tryFind s
            |> Option.map (fun data -> tabPaths remainingPath data)
            |> defaultArg <| Map.empty
        | (All::remainingPath, Grouped m) ->
            m
            |> Map.map
                (fun key data ->
                    let paths = tabPaths remainingPath data
                    paths
                    |> Map.toList
                    |> List.map
                        (fun (innerKey, innerPath) ->
                            ((if innerKey = "" then key else key + " - " + innerKey), (Specific key)::innerPath)))
            |> Map.toList
            |> List.collect snd
            |> Map.ofList
        | _ -> Map.empty

    /// Build divergences from data.
    let rec collapseDivergences (data:Data) : Data =
        match data with
        | Grouped m ->
            m
            |> Map.toList
            |> List.collect
                (fun (key,data) ->
                    match collapseDivergences data with
                    | Grouped m2 ->
                        m2
                        |> Map.toList
                        |> List.map (fun (key2, data2) -> (key + " - " + key2, data2))
                    | Invalid ->
                        []
                    | d ->
                        [(key, d)])
            |> function | [] -> Invalid | li -> li |> Map.ofList |> Grouped
        | _ -> data
