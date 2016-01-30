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
    
    type Course = {
        nrc: int
        id: CourseId
        name: string
        category: string
        teacher: string
        credits: string
    }
    and CourseId = {
        subject: string
        number: string
    }
    
    type Schedule = seq<CourseTimetable>
    and CourseTimetable = {
        course: Course
        schedule: seq<CourseSchedule>
    }
    and CourseSchedule = {
        room: CourseRoom
        time: CourseTime
        duration: CourseDuration
    }
    and CourseRoom =
        | Room of string
        | NoRoom
    and CourseTime = {
        day: DayOfWeek
        fromHour: TimeSpan
        toHour: TimeSpan
    }
    and CourseDuration = {
        fromDate: DateTime
        toDate: DateTime
    }