namespace Pilule.Core

module Capsule =
  
    open System

    type Session = { 
        idul: string
        password: string
        session: SessionCookie option 
    } with
        static member isLoggedIn session =
            Option.isSome session.session
    and SessionCookie = string

    type Semester = { 
        season: Season
        year: Year 
    } with 
        static member toCapsuleFormat semester =
            sprintf "%i%s" semester.year (Season.toMonth semester.season)
    and Season =
        | Autumn
        | Winter
        | Summer with 
        static member toMonth season =
            match season with
            | Winter -> "01"
            | Summer -> "05"
            | Autumn -> "09"
    and Year = int
    
    type RequestConfiguration = {
        Host: string
        SessionToken: string
    }