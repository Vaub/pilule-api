namespace Pilule.Api

module Course =
    
    open Suave
    open Suave.Operators
    open Suave.Successful
    open Newtonsoft.Json
    
    open Utils
    open Pilule.Core.Capsule
    open Pilule.Core.Course
    open Pilule.Core.Auth
    
    let extractTitle (request: HttpRequest) =
        match request.queryParam "name" with
        | Choice1Of2 p -> p
        | Choice2Of2 _ -> ""
    
    let createSubject subject title =
        { semester = extractSemester ""; mode = Title ([subject], title)  }
    
    let createCourseSign subject number = 
        { semester = extractSemester ""; mode = Sign (subject, number) }
    
    let internal searchCoursesWebPart session subject course: WebPart =
        fun ctx ->
            async {
                let title = extractTitle ctx.request
                let courses = 
                    match course with
                    | Some c -> findCourses (createCourseSign subject c) session.session
                    | None -> findCourses (createSubject subject title) session.session
                return!
                    (OK (JsonConvert.SerializeObject courses) >=> jsonType) ctx
            }
        
    
    let searchCourses subject (course: string option): WebPart =
        ulavalSession <|
        fun session ->
            searchCoursesWebPart session subject course