namespace Pilule.Core

module Requests =

    open FSharp.Data
    open Capsule
    
    open Utils.Computation
    
    [<Literal>]
    let host = "https://capsuleweb.ulaval.ca"
    
    type RequestBuilder() =
        member this.Bind(x, f) =
            match x with
            | None -> None
            | Some r -> f r
        member this.Return(x) = Some x
        member this.Delay(f) = f
    let requests = new RequestBuilder()
    
    let extractResponseBody r =
        match r with
        | Some response -> 
            match response.Body with
            | Text t -> Some t
            | _ -> None
        | None -> None
                
    let runRequestAsync r =
        async {
            try
                let! response = r ()
                return Some response
            with
                ex -> return None
        }