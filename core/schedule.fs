namespace Pilule.Core

module Schedule =
    
    open System
    
    open Capsule
    open Course
    
    type Schedule = {
        Semester: Semester
        Timetable: (Course * CourseTimetable) seq
    }
    and CourseTimetable = {
        Dates: TimetableDate seq
    }
    and TimetableDate = {
        StartTime: TimeSpan
        Duration: TimeSpan
        DayOfWeek: DayOfWeek
    }
    
    module Parser =
    
        ()
    
    module Fetcher =
        
        open FSharp.Data
        
        open Requests
        
        [<Literal>]
        let endpoint = "/pls/etprod8/bwskfshd.P_CrseSchdDetl"
        
        let internal scheduleRequest s c =
            fun () ->
                Http.AsyncRequest (
                    url = host + endpoint, httpMethod = HttpMethod.Post,
                    body = FormValues [("term_in", s |> Semester.toCapsuleFormat)],
                    cookies = [("SESSID", c.SessionToken)] )
                    
        let asyncCapsuleFetcher s c =
            let request = scheduleRequest s c
            async {
                let! response = runRequestAsync request
                return
                    match response with
                    | Response r -> Some r
            }
            seq []