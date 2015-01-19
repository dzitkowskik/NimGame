module GUI
open System.Windows.Forms 
open System.Drawing 

let windowWidth = 512
let windowHeight = 512

let labelFont = new Font("Times New Roman", 16.0f, FontStyle.Bold);

let logEnd = 470-windowWidth/2;

// The window part

let window =
  new Form(Text="Web Source Length", Size=Size(windowWidth,windowHeight),
    MinimumSize=Size(windowWidth,windowHeight), MaximumSize=Size(windowWidth,windowHeight),
    FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false)

let logBox = 
    new TextBox(Location=Point(windowWidth/2,windowHeight/2),
        Size=Size(logEnd,windowHeight/2-30), Multiline=true,
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

let heapMoveLabel = new Label(Location=Point(windowWidth/2+50,50), Size=Size(50,50), Text="Heap")
let heapMoveBox =
    new NumericUpDown(Location=Point(windowWidth/2+100,50),Size=Size(100,50),
              MaximumSize=Size(100,50), Value=1m, Increment=1m, DecimalPlaces=0,
              Minimum=0m, Maximum=20m)
heapMoveBox.KeyPress.Add (fun e -> if isNumKey e then e.Handled <- true)

let numMoveLabel = new Label(Location=Point(windowWidth/2+50,100), Size=Size(50,50), Text="Matches number")
let numMoveBox =
    new NumericUpDown(Location=Point(windowWidth/2+100,100),Size=Size(100,50),
              MaximumSize=Size(100,50), Value=1m, Increment=1m, DecimalPlaces=0,
              Minimum=0m, Maximum=20m)
numMoveBox.KeyPress.Add (fun e -> if isNumKey e then e.Handled <- true)

let moveButton =
  new Button(Location=Point(windowWidth/2+100,150),MinimumSize=Size(100,50),
              MaximumSize=Size(100,50),Text="MOVE")

let urlLabel = new Label(Location=Point(50,windowHeight/2-50), Size=Size(150,20), Text="Fetch game from url:")
let urlBox =
  new TextBox(Location=Point(50,windowHeight/2-30),MinimumSize=Size(420,20),
              MaximumSize=Size(700,50), Text="http://localhost:8080/" )

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


// Initialization
window.Controls.Add heapMoveLabel
window.Controls.Add numMoveLabel
window.Controls.Add urlLabel
window.Controls.Add logBox
window.Controls.Add numMoveBox
window.Controls.Add heapMoveBox
window.Controls.Add startButton
window.Controls.Add moveButton
window.Controls.Add cancelButton
window.Controls.Add heapsLabel
window.Controls.Add urlBox