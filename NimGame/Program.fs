module Program 

open System 
open System.Text
open System.Net 
open System.Threading 
open System.Windows.Forms 
open System.Drawing 

open Game

let windowWidth = 512
let windowHeight = 512

let logBuilder = System.Text.StringBuilder()
let sisde = System.Random().Next()%2

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
  new Form(Text="Web Source Length", Size=Size(windowWidth,windowHeight))

let logBox = 
    new TextBox(Location=Point(windowWidth/2,windowHeight/2),
        Size=Size(windowWidth/2-50,windowHeight/2-50), Multiline=true,
        ScrollBars = ScrollBars.Vertical)

let heapsLabel = 
    new Label(Location=Point(20,20), Size=Size(windowWidth/3,windowHeight/3), Font=labelFont)

let startButton =
  new Button(Location=Point(50,windowHeight/2),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="START")

let cancelButton =
  new Button(Location=Point(50,windowHeight/2+100),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="CANCEL")

let heapMoveBox =
  new TextBox(Location=Point(windowWidth/2+100,50),Size=Size(100,50),
              MaximumSize=Size(100,50))

let numMoveBox =
  new TextBox(Location=Point(windowWidth/2+100,100),Size=Size(100,50),
              MaximumSize=Size(100,50))

let moveButton =
  new Button(Location=Point(windowWidth/2+100,150),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="MOVE")

let disable bs = 
    for b in [startButton;cancelButton;moveButton] do 
        b.Enabled  <- true
    for (b:Button) in bs do 
        b.Enabled  <- false

// An enumeration of the possible events 
type Message =
  | Begin
  | Move of int * int
  | End of bool
  | Error 
  | Cancelled

//exception UnexpectedMessage

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
        disable [cancelButton; moveButton]
        let! msg = ev.Receive()
        match msg with
        | Begin   -> 
            updateLog "New game started"
            let side = System.Random().Next()%2
            let! matches = getMatches
            heapsLabel.Text <- getHeapsText matches
            if side=0 then return! aiMove(matches)
            else return! pMove(matches)
        | Cancelled -> return! ready()
        | _         -> failwith("ready: unexpected message")
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
         let! msg = ev.Receive()
         match msg with
         | Begin ->
             ev.Post Begin
             return! ready()
         | _     ->  failwith("finished: unexpected message")
     }

// Initialization
window.Controls.Add logBox
window.Controls.Add numMoveBox
window.Controls.Add heapMoveBox
window.Controls.Add startButton
window.Controls.Add moveButton
window.Controls.Add cancelButton
window.Controls.Add heapsLabel
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

