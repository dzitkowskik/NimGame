module Game

open System
open System.Threading

let rnd = System.Random()

let getMatches = 
    async{
        let count = rnd.Next()%5 + 2 
        return List.init count (fun _ -> rnd.Next()%100)}

let maxIndex l =  
    l
    |> List.mapi (fun i x -> i, x)
    |> List.maxBy snd 
    |> fst

let makeAiMove matches =
    let m = matches |> List.fold (fun acc elem -> acc ^^^ elem) 0
    if m=0 then (maxIndex matches, 1)
    else
        let temp = List.map (fun x -> x ^^^ m) matches
        let k = List.zip matches temp |> List.findIndex (fun (x, y) -> x > y)
        let value = List.nth matches k
        (k, value - (value^^^m))

let checkMove (heap, num) matches =
    let value = List.nth matches heap
    if (value-num)>=0 then true
    else false

let checkWin matches = 
    matches |> List.forall (fun x -> x=0)

let applyMove (heap, num) matches =
    matches |> List.mapi (fun i x -> if heap=i then x-num else x)

let getHeapsText matches = 
    matches 
    |> List.mapi (fun i x -> i.ToString()+") "+x.ToString())
    |> String.concat "\n"
