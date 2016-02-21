namespace Pilule.Core

module ICalendar = 

    open System
    open System.Text
    
    open Capsule
    open Schedule
    
    let internal convertDate (date: DateTime) =
        date.ToString("yyyyMMddTHHmmssfffZ")
    
    let internal convertWeekDay (day: DayOfWeek) =
        match day with
        | Sunday -> "SU"
        | Monday -> "MO"
        | Tuesday -> "TU"
        | Wednesday -> "WE"
        | Thursay -> "TH"
        | Friday -> "FR"
        | Saturday -> "SA"
    
    let internal addLine (line: string) (builder: StringBuilder) =
        ignore <| builder.Append line
        ignore <| builder.AppendLine()
        builder
        
    let internal asString (builder: StringBuilder) =
        builder.ToString()
    
    type ICalendar = {
        id: string
        name: string
        events: seq<ICalendarEvent>
    } with
        static member toICalString calendar =
            StringBuilder()
            |> addLine "BEGIN:VCALENDAR"
            |> addLine (sprintf "PROCID:%s" calendar.id)
            |> fun x ->
                calendar.events
                |> Seq.map (fun e -> ICalendarEvent.toICalString e)
                |> Seq.iter (fun e -> x |> addLine e |> ignore)
                x
            |> addLine "END:VCALENDAR"
            
    and ICalendarEvent = {
        uid: string
        dtstart: DateTime
        summary: string
        location: string
        recurrence: WeeklyFrequency
    } with
        static member toICalString event: string =
            StringBuilder()
            |> addLine "BEGIN:VEVENT"
            |> addLine (sprintf "UID:%s" event.uid)
            |> addLine (sprintf "DTSTAMP:%s" (convertDate DateTime.Now))
            |> addLine (sprintf "DTSTART:%s" (convertDate event.dtstart))
            |> addLine (sprintf "RRULE:%s" (WeeklyFrequency.toICalString event.recurrence))
            |> addLine (sprintf "SUMMARY:%s" event.summary)
            |> addLine (sprintf "LOCATION:%s" event.summary)
            |> addLine "END:VEVENT"
            |> asString
            
    and WeeklyFrequency = {
        day: DayOfWeek
        until: DateTime
    } with
        static member toICalString week =
            sprintf "FREQ=WEEKLY;BYDAY=%s;UNTIL=%s" (convertWeekDay week.day) (convertDate week.until)
    
    //let internal createEvent (course: Course) (schedule: CourseSchedule) =
    //
    //let createFrom (schedule: Schedule) =
    //    ""