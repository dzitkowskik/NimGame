module Program 

open System 
open System.Net 
open System.Threading 
open System.Windows.Forms 
open System.Drawing 

open Game

let windowWidth = 1024
let windowHeight = 1024
let log = "log:\n"

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

// The window part
let window =
  new Form(Text="Web Source Length", Size=Size(windowWidth,windowHeight))

let logBox =
  new TextBox(Location=Point(512,512),Size=Size(400,400))

let heapMoveBox =
  new TextBox(Location=Point(600,100),Size=Size(100,50),
              MaximumSize=Size(100,50))

let numMoveBox =
  new TextBox(Location=Point(600,250),Size=Size(100,50),
              MaximumSize=Size(100,50))

let startButton =
  new Button(Location=Point(50,650),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="START")

let cancelButton =
  new Button(Location=Point(50,750),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="CANCEL")

let moveButton =
  new Button(Location=Point(800,100),MinimumSize=Size(100,50),
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

let rec ready() = 
    async {
        logBox.Text <- log
        disable [cancelButton; moveButton]
        let! msg = ev.Receive()
        match msg with
        | Begin   -> 
            let side = playerStarting
            if side=0 then return! aiMove(getMatches)
            else return! pMove(getMatches)
        | Cancelled -> return! ready()
        | _         -> failwith("ready: unexpected message")}
  
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
             let m = applyMove (h, c) matches
             if checkWin(m) then return! finished("AI")
             else return! pMove(m)
         | Error      -> return! finished("Error")
         | Cancelled  -> 
            ts.Cancel()
            return! cancelling()
         | _          -> failwith("loading: unexpected message")}

and pMove(matches) =
    async {
    disable [startButton] 

    let! msg = ev.Receive()
    match msg with
    | Move(h, c) ->
        let m = applyMove (h, c) matches
        if checkWin(m) then return! finished("P")
        else return! aiMove(m)
    | Error      -> return! finished("Error")
    | Cancelled  -> return! cancelling()
    | _          -> failwith("loading: unexpected message")}

and cancelling() =
  async {
         
         disable [startButton; moveButton; cancelButton]
         let! msg = ev.Receive()
         match msg with
         | Cancelled | Error | Move _ ->
                   return! finished("Cancelled")
         | _    ->  failwith("cancelling: unexpected message")}

and finished(s) =
  async {
         
         disable [moveButton; cancelButton]
         let! msg = ev.Receive()
         match msg with
         | Begin -> return! ready()
         | _     ->  failwith("finished: unexpected message")}

// Initialization
window.Controls.Add logBox
window.Controls.Add numMoveBox
window.Controls.Add heapMoveBox
window.Controls.Add startButton
window.Controls.Add moveButton
window.Controls.Add cancelButton
startButton.Click.Add (fun _ -> ev.Post (Begin))
moveButton.Click.Add (fun _ -> ev.Post (Move (Convert.ToInt32(heapMoveBox.Text), Convert.ToInt32(numMoveBox.Text))))
cancelButton.Click.Add (fun _ -> ev.Post Cancelled)

// Start
Async.StartImmediate (ready())
Application.Run(window)
// window.Show()

