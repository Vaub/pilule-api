namespace Pilule.Core

module Auth =
  
    open System.Net
    open FSharp.Data
    open Utils.Strings

    open Requests
    open Capsule

    [<Literal>]
    let loginForm = "/pls/etprod8/twbkwbis.P_WWWLogin"
    [<Literal>]
    let loginEndpoint = "/pls/etprod8/twbkwbis.P_ValLogin"
    [<Literal>]
    let menuEndpoint = "/pls/etprod8/twbkwbis.P_GenMenu?name=bmenu.P_MainMnu"

    type LoginResponse =
        | Success of Session
        | Error of LoginError
    and LoginError =
        | WrongIdentifiers
        | ErrorContactingServer
        | Unknown

    let login session: LoginResponse =
        let pingUrl = (Requests.host + loginForm)

        let extractSessionCookie body: SessionCookie option =
            match body with
            | FirstRegexGroup ".*Set-Cookie: SESSID=(.*);.*" cookie -> Some cookie
            | _ -> None

        let loginUrl = (host + loginEndpoint)
        let formData = [("sid", session.idul); ("PIN", session.password)]
        let cc = CookieContainer();

        let initialRequest =
            fun unit ->
                Http.Request (
                    url = pingUrl, httpMethod = HttpMethod.Get,
                    cookieContainer = cc
                )

        let loginRequest =
            fun unit -> 
                Http.Request (
                    url = loginUrl, httpMethod = HttpMethod.Post, 
                    cookies = [("SESSID", "")],
                    cookieContainer = cc, 
                    body = FormValues formData)

        let response =      
            requests {
                let! tryConnect = runRequest initialRequest
                let! response = runRequest loginRequest
                return response
            }

        match response with
        | Response r ->
            let cookie =
                maybe {
                let! body = extractBody r.Body
                let! cookie = extractSessionCookie body
                return cookie
            }

            match cookie with
            | Some c -> Success { session with session = Some c }
            | None -> Error LoginError.WrongIdentifiers
        | _ -> Error LoginError.ErrorContactingServer
    
    
