namespace Pilule.Core

[<AutoOpen>]
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
    }
    and Season =
        | Autumn
        | Winter
        | Summer
    and Year = int

    type Capsule = { 
        login: Session -> CapsuleResponse<Session>
        findSchedule: Session -> Schedule
        findSemesterSchedule: Semester -> Session -> Schedule 
    }
    and Schedule = seq<string>
    and CapsuleResponse<'t> =
        | Success of 't
        | Error of ResponseError
    and ResponseError =
        | CouldNotContactCapsule
        | WrongIdentifiers
        | Unknown of string