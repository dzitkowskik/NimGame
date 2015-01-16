module Game

open System

let getMatches = 
    let rnd = System.Random()
    let count = rnd.Next()%5 + 2 
    List.init count (fun _ -> rnd.Next()%100)

let makeAiMove matches =
    // TODO: AI intelligence and move making
    (0,0)

let checkWin matches = 
    // TODO: Check if someone wins/looses
    false

let applyMove (heap, num) matches =
    matches |> List.mapi (fun i x -> if heap=i then x-num else x)

let getHeapsText matches = 
    matches 
    |> List.mapi (fun i x -> i.ToString()+") "+x.ToString())
    |> String.concat "\n"
