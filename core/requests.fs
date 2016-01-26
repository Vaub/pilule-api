namespace Pilule.Core

module Requests =
  
  open FSharp.Data
  
  type RequestResult =
    | Response of HttpResponse
    | Error
  
  type MaybeBuilder() =
    member this.Bind(x, f) =
      match x with
      | None -> None
      | Some v -> f v
    member this.Return(x) = Some x
  let maybe = new MaybeBuilder()
  
  type RequestBuilder() =
    member this.Bind(x, f) =
      match x with
      | Error -> Error
      | Response r -> f r
    member this.Return(x) = Response x
    member this.Delay(f) = f()
  let requests = new RequestBuilder()
  
  let extractBody body =
    match body with
    | Text t -> Some t
    | _ -> None
  
  let runRequest request =
    try 
      Response (request())
    with 
      ex -> 
        printfn "Error running request: %A" (ex.Message)
        Error
  
  let inline pingRequest host = 
    fun unit -> Http.Request (url = host, httpMethod = HttpMethod.Get)
  
  let ping host =
    let pingUrl = host
    let request = pingRequest host
    
    match (runRequest request) with
    | Response r -> 
      printfn "Status for %A is %A" host r.StatusCode
      r.StatusCode = 200
    | Error -> false