module Game

let getMatches = 
    let rnd = System.Random()
    let count = rnd.Next()
    List.init count (fun _ -> rnd.Next()%100)

let playerStarting =
    let rnd = System.Random()
    rnd.Next()%2

let makeAiMove matches =
    // TODO: AI intelligence and move making
    (0,0)

let checkWin matches = 
    // TODO: Check if someone wins/looses
    false

let applyMove (heap, num) matches =
    matches |> List.mapi (fun i x -> if heap=i then x-num else x)

let getHeapsText matches = 

