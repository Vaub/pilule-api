namespace Pilule.Api

module Utils =
    
    open System
    
    open Newtonsoft.Json
    open Suave
    open Suave.Operators
    open Suave.Writers
    open Suave.Json
    open Suave.Utils
    
    open Pilule.Core.Auth
    open Pilule.Core.Capsule
    open Pilule.Core.Utils.Strings
    
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
    
    let internal matchLoginError error =
        match error with
        | LoginError.WrongIdentifiers ->
            RequestErrors.UNAUTHORIZED "Wrong identifiers, please try again"
        | LoginError.ErrorContactingServer ->
            ServerErrors.SERVICE_UNAVAILABLE "Unable to contact ULaval servers"
        | _ ->
            ServerErrors.INTERNAL_ERROR "Unknown error happened, please try again"
    
    let jsonType: WebPart =
        setMimeType "application/json; charset=utf-8"
    
    let ulavalSession (f: Session -> WebPart): WebPart =
        fun ctx ->
            async {
                let session = extractSession ctx
                return!
                    match session with
                    | LoginResponse.Success s ->
                        f s ctx
                    | LoginResponse.Error e ->
                        matchLoginError e ctx
            }
    
    let currentSemester =
        let now = DateTime.Now
        
        let year = now.Year
        let season = 
            match now.Month with
            | m when m < 5 -> Season.Winter
            | m when m < 9 -> Season.Summer
            | _ -> Season.Autumn
        { year = year; season = season }
    
    let extractSemester s =
        match s with
        | ParseRegex "([WwSsAa])([0-9]{4})" [season; year]
            -> 
            let semesterSeason =
                match season.ToLower() with
                | "w" -> Season.Winter
                | "s" -> Season.Summer
                | _ -> Season.Autumn
            { year = int year; season = semesterSeason }
        | _ -> currentSemester