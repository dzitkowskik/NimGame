module GUI
open System.Windows.Forms 
open System.Drawing 

let windowWidth = 512
let windowHeight = 512

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

let urlBox =
  new TextBox(Location=Point(50,windowHeight/2-30),MinimumSize=Size(420,20),
              MaximumSize=Size(700,50))


              // Initialization
window.Controls.Add logBox
window.Controls.Add numMoveBox
window.Controls.Add heapMoveBox
window.Controls.Add startButton
window.Controls.Add moveButton
window.Controls.Add cancelButton
window.Controls.Add heapsLabel
window.Controls.Add urlBox

