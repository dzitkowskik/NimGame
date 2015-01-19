module Program 

open System 
open System.Text
open System.Net 
open System.Threading 
open System.Windows.Forms 
open System.Drawing 

open Game
open GUI
open EventQueue

let logBuilder = System.Text.StringBuilder()

let sisde = System.Random().Next()%2

//Expected syntax: A list of numbers, seperated by white space. Every number is the amount of matches in a heap
let rec parseNimWebsite (text:string) heaps =
    let i = text.IndexOf(" ")
    //printfn "%A" i
    if i <> -1 then  
        let nr = int (text.Substring( 0, i))
        let rest = text.Substring( i + 1 )
        let newHeaps = nr::heaps
        parseNimWebsite rest newHeaps
    else
        (int text)::heaps

// An enumeration of the possible events 
type Message =
  | Begin
  | Move of int * int
  | End of bool
  | Error 
  | Cancelled
  | HTML of string

// The dialogue automaton 
let ev = AsyncEventQueue()

let updateLog text =
    logBuilder.AppendLine(text) |> ignore
    logBox.Text <- logBuilder.ToString()
    logBox.SelectionStart <- logBox.Text.Length
    logBox.ScrollToCaret()


let mutable aiTease = true

let rec ready() = 
    async {
        logBuilder.Clear() |> ignore
        disable [cancelButton; moveButton;]
        disableNum [numMoveBox; heapMoveBox]
        let! msg = ev.Receive()
        match msg with
        | Begin   -> 
            updateLog "New game started"
            let side = System.Random().Next()%2
            if urlBox.Text.Trim().Length = 0 then
                let! matches = getMatches
                heapsLabel.Text <- getHeapsText matches
                if side=0 then return! aiMove(matches)
                else return! pMove(matches)
            else
                return! fetching(urlBox.Text)
                
        | Cancelled -> return! ready()
        | _         -> failwith("ready: unexpected message")
    }

and fetching(url) =
    async{
        //ansBox.Text <- "Downloading"
        use ts = new CancellationTokenSource()
        Async.StartWithContinuations
           (async {
            let webCl = new WebClient()
            let! html = webCl.AsyncDownloadString(Uri url)
            return html },
                (fun html -> ev.Post (HTML html)) , // normal termination
                (fun _ -> ev.Post Error) , // error (e.g. wrong url)
                (fun _ -> ev.Post Cancelled) , // cancellation
                ts.Token)

        disable [startButton; moveButton]

        let! msg = ev.Receive()
        match msg with
        | HTML htm -> let side = System.Random().Next()%2
                      let matches = parseNimWebsite htm []
                      heapsLabel.Text <- getHeapsText matches
                      if side=0 then return! aiMove(matches)
                      else return! pMove(matches)
        | Error -> return! finished("","Error")
        | Cancelled -> 
            ts.Cancel()
            return! cancelling()
        | _ -> failwith("loading: unexpected message")
    }
and aiMove(matches) =
    async {
         use ts = new CancellationTokenSource()

          // start the load
         Async.StartWithContinuations
             (async { return makeAiMove(matches) },
              (fun result ->
                  if(aiTease && snd result) then
                    updateLog("AI: you will lose >:)")
                    aiTease <- false
                  ev.Post (Move (fst result))),
              (fun _ -> ev.Post Error),
              (fun _ -> ev.Post Cancelled),
              ts.Token)

         disable [startButton; moveButton]   
         disableNum []

         let! msg = ev.Receive()
         match msg with
         | Move(h, c) ->
             if checkMove (h, c) matches then
                 updateLog ("Ai subtracted "+c.ToString()+" from heap "+h.ToString())
                 let m = applyMove (h, c) matches
                 heapsLabel.Text <- getHeapsText m
                 if checkWin(m) then return! finished("AI","")
                 else return! pMove(m)
             else return! finished("Player","AI made wrong move!")
         | Error      -> return! finished("","Error")
         | Cancelled  -> 
            ts.Cancel()
            return! cancelling()
         | _          -> failwith("loading: unexpected message")
     }
and pMove(matches) =
    async {
        disable [startButton] 
        disableNum []

        let! msg = ev.Receive()
        match msg with
        | Move(h, c) ->
            if checkMove (h, c) matches then
                updateLog ("Player subtracted "+c.ToString()+" from heap "+h.ToString())
                let m = applyMove (h, c) matches
                heapsLabel.Text <- getHeapsText m
                if checkWin(m) then return! finished("Player","")
                else return! aiMove(m)
            else return! finished("AI","Player made wrong move!")
        | Error      -> return! finished("","Error")
        | Cancelled  -> return! cancelling()
        | _          -> failwith("loading: unexpected message")
    }
and cancelling() =
    async {
         updateLog ("Game cancelled")

         disable [moveButton; cancelButton]
         disableNum [numMoveBox; heapMoveBox]

         let! msg = ev.Receive()
         match msg with
         | Begin -> 
             ev.Post Begin
             return! ready()
         | Cancelled -> return! ready()
         | _     ->  failwith("cancelling: unexpected message")
     }
and finished(s, err) =
    async {
         if err<> "" then updateLog (err)
         if s<> "" then updateLog ("Game won by "+s)

         disable [moveButton; cancelButton]
         disableNum [numMoveBox; heapMoveBox]

         let! msg = ev.Receive()
         match msg with
         | Begin ->
             ev.Post Begin
             return! ready()
         | _     ->  failwith("finished: unexpected message")
     }

startButton.Click.Add (fun _ -> if startButton.Enabled then ev.Post (Begin))
moveButton.Click.Add (fun _ -> 
    if moveButton.Enabled then
        ev.Post (Move (Convert.ToInt32(heapMoveBox.Text), Convert.ToInt32(numMoveBox.Text))))
cancelButton.Click.Add (fun _ -> 
    if cancelButton.Enabled then
        ev.Post Cancelled)

// Start
Async.StartImmediate (ready())
Application.Run(window)
// window.Show()