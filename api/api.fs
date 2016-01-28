namespace Pilule.Api

module app =
    
    open System
    open System.Text
    open Suave
    open Suave.Json
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful
    open Suave.Utils
    open Newtonsoft.Json
    
    open Pilule.Core.Utils
    open Pilule.Core.Auth
    open Pilule.Core.Capsule
    open Pilule.Core.Schedule
    
    open Pilule.Api.Schedule
    
    let extractBasicInfo (token: string) =
        let parts = token.Split (' ')
        let enc = parts.[1].Trim()
        let decoded = ASCII.decodeBase64 enc
        let indexOfColon = decoded.IndexOf(':')
        (decoded.Substring(0,indexOfColon), decoded.Substring(indexOfColon+1))
    
    let extractSession (ctx: HttpContext) =
        let header = ctx.request.header "authorization"
        match header with
        | Choice1Of2 h ->
            let (idul, pwd) = extractBasicInfo h
            let session = { idul = idul; password = pwd; session = None }
            login session
        | _ -> LoginResponse.Error LoginError.Unknown
            
    let getSchedule semester (session: Session) =
        findSchedule semester session.session
    
    let schedulePart semester: WebPart = 
        fun ctx ->
            async {
                let session = extractSession ctx
                return!
                    match session with
                    | LoginResponse.Success s ->
                        let semesterFound = extractSemester semester
                        OK (JsonConvert.SerializeObject (getSchedule semesterFound s)) ctx
                    | LoginResponse.Error e ->
                        RequestErrors.FORBIDDEN (sprintf "%A" e) ctx
            }
    
    let schedule =
        choose
            [ GET >=> choose
                [ pathScan "/schedule/%s" (fun s -> schedulePart s)
                  path "/schedule" >=> (schedulePart "") ] ]
    
    let app = schedule
    
    [<EntryPoint>]
    let main args =
        startWebServer defaultConfig app
        0