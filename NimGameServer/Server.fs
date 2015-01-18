open System
open System.Net
open System.Text
open System.IO

open Game
 
let host = "http://localhost:8080/"
 
let listener (handler:(HttpListenerRequest->HttpListenerResponse->Async<unit>)) =
    let hl = new HttpListener()
    hl.Prefixes.Add host
    hl.Start()
    let task = Async.FromBeginEnd(hl.BeginGetContext, hl.EndGetContext)
    async {
        while true do
            let! context = task
            Async.Start(handler context.Request context.Response)
    } |> Async.Start
 
let output (req:HttpListenerRequest) = 
   async {
       let! matches = getMatches
       return matches |> List.map (fun x -> x.ToString()) |> String.concat " "
   }
 
listener (fun req resp ->
    async {
        let! out = output req
        let txt = Encoding.ASCII.GetBytes(out)
        resp.ContentType <- "text/html"
        resp.OutputStream.Write(txt, 0, txt.Length)
        resp.OutputStream.Close()
    })

printfn "To stop the server press any key..."
Console.ReadLine() |> ignore    