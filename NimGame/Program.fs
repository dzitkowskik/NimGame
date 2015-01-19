﻿module Program 

open System 
open System.Text
open System.Net 
open System.Threading 

open Game
open GUI
open EventQueue

let windowWidth = 512
let windowHeight = 512

let logBuilder = System.Text.StringBuilder()
let sisde = System.Random().Next()%2

//Expected syntax: A list of numbers, seperated by white space. Every number is the amount of matches in a heap
let rec parseNimWebsite (text:string) heaps =
    let i = text.IndexOf(" ")
    printfn "%A" i
    if i <> -1 then  
        let nr = int (text.Substring( 0, i))
        let rest = text.Substring( i + 1 )
        let newHeaps = nr::heaps
        parseNimWebsite rest newHeaps
    else
        (int text)::heaps

// An asynchronous event queue kindly provided by Don Syme 
type AsyncEventQueue<'T>() = 
    let mutable cont = None 
    let queue = System.Collections.Generic.Queue<'T>()
    let tryTrigger() = 
        match queue.Count, cont with 
        | _, None -> ()
        | 0, _ -> ()
        | _, Some d -> 
            cont <- None
            d (queue.Dequeue())

    let tryListen(d) = 
        if cont.IsSome then invalidOp "multicast not allowed"
        cont <- Some d
        tryTrigger()

    member x.Post msg = queue.Enqueue msg; tryTrigger()
    member x.Receive() = 
        Async.FromContinuations (fun (cont,econt,ccont) -> 
            tryListen cont)

let labelFont = new Font("Times New Roman", 16.0f, FontStyle.Bold);

// The window part
let window =
  new Form(Text="Web Source Length", Size=Size(windowWidth,windowHeight),
    MinimumSize=Size(windowWidth,windowHeight), MaximumSize=Size(windowWidth,windowHeight),
    FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false)

let logBox = 
    new TextBox(Location=Point(windowWidth/2,windowHeight/2),
        Size=Size(windowWidth/2-50,windowHeight/2-50), Multiline=true,
        ScrollBars = ScrollBars.Vertical, ReadOnly=true)

let heapsLabel = 
    new Label(Location=Point(20,20), Size=Size(windowWidth/3,windowHeight/3),
        Font=labelFont)

let startButton =
  new Button(Location=Point(50,windowHeight/2),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="START")

let cancelButton =
  new Button(Location=Point(50,windowHeight/2+100),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="CANCEL")

let isNumKey (e:KeyPressEventArgs) = 
    let back = (int Keys.Back)
    let key = int e.KeyChar
    key<>back && (key < 48 || key > 57)

let heapMoveBox =
    new NumericUpDown(Location=Point(windowWidth/2+100,50),Size=Size(100,50),
              MaximumSize=Size(100,50), Value=1m, Increment=1m, DecimalPlaces=0,
              Minimum=0m, Maximum=20m)
heapMoveBox.KeyPress.Add (fun e -> if isNumKey e then e.Handled <- true)

let numMoveBox =
    new NumericUpDown(Location=Point(windowWidth/2+100,100),Size=Size(100,50),
              MaximumSize=Size(100,50), Value=1m, Increment=1m, DecimalPlaces=0,
              Minimum=0m, Maximum=20m)
numMoveBox.KeyPress.Add (fun e -> if isNumKey e then e.Handled <- true)

let moveButton =
  new Button(Location=Point(windowWidth/2+100,150),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="MOVE")

let urlBox =
  new TextBox(Location=Point(50,windowHeight/2-30),MinimumSize=Size(420,20),
              MaximumSize=Size(700,50))

let disable bs = 
    for b in [startButton;cancelButton;moveButton] do 
        b.Enabled  <- true
    for (b:Button) in bs do 
        b.Enabled  <- false
       
let disableNum n =
    for x in [numMoveBox; heapMoveBox] do
        x.Enabled <- true
    for (y:NumericUpDown) in n do 
        y.Enabled  <- false

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

        disable [startButton]

        let! msg = ev.Receive()
        match msg with
        | HTML htm -> let side = System.Random().Next()%2
                      let matches = parseNimWebsite htm []
                      heapsLabel.Text <- getHeapsText matches
                      if side=0 then return! aiMove(matches)
                      else return! pMove(matches)
        | Error -> return! finished("","Error")
        | Cancel -> ts.Cancel()
                    return! cancelling()
        | _ -> failwith("loading: unexpected message")
    }
    
and aiMove(matches) =
    async {
         use ts = new CancellationTokenSource()

          // start the load
         Async.StartWithContinuations
             (async { return makeAiMove(matches) },
              (fun result -> ev.Post (Move result)),
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

