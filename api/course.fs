namespace Pilule.Api

module Course =
    
    open Suave
    open Suave.Operators
    open Suave.Successful
    open Newtonsoft.Json
    open FSharp.Data
    
    open Utils
    open Pilule.Core.Utils.Computation
    open Pilule.Core.Capsule
    open Pilule.Core.Course
    open Pilule.Core.Auth
    
    let extractTitle (request: HttpRequest) =
        match request.queryParam "name" with
        | Choice1Of2 p -> p
        | Choice2Of2 _ -> ""
    
    let createSubject subject title =
        { Semester = extractSemester ""; Mode = ByName (title, [subject])  }
    
    let createCourseSign subject number = 
        { Semester = extractSemester ""; Mode = BySign { Subject = subject; Number = number } }
    
    let createRequestConfig s = {
        Host = "https://capsuleweb.ulaval.ca"
        SessionToken = s
    }
    
    let finder = {
        Fetcher = Query.queryCapsuleAsync
        Parser = Parser.capsuleParser
    }
    
    let internal searchCoursesWebPart session subject course: WebPart =
        fun ctx ->
            async {
                let title = extractTitle ctx.request
                let coursesAsync =
                    maybe {
                        let! s = session.session
                        let requestConfig = (createRequestConfig s)
                        return
                            match course with
                            | Some c -> finder.FindCoursesAsync (createCourseSign subject c) requestConfig
                            | None -> finder.FindCoursesAsync (createSubject subject title) requestConfig
                    }
                
                let! courses =
                    match coursesAsync with 
                    | Some s -> s
                    | None -> async { return seq [] }
                return!
                    (OK (JsonConvert.SerializeObject courses) >=> jsonType) ctx
            }
        
    
    let searchCourses subject (course: string option): WebPart =
        ulavalSession <|
        fun session ->
            searchCoursesWebPart session subject course