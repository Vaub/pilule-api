namespace Pilule.Api

module app =
    
    open System
    open System.Text
    open Suave
    open Suave.Json
    open Suave.Filters
    open Suave.Writers
    open Suave.Operators
    open Suave.Successful
    open Suave.RequestErrors
    open Suave.Utils
    open Newtonsoft.Json
    
    open Pilule.Core.Utils
    open Pilule.Core.Auth
    open Pilule.Core.Capsule
    open Pilule.Core.Schedule
    
    open Pilule.Api.Utils
    open Pilule.Api.Course
    
    let schedulePart semester: WebPart = 
        ulavalSession <|
        fun session -> 
            let semesterFound = extractSemester semester
            OK (JsonConvert.SerializeObject (findSchedule semesterFound session.session)) >=> jsonType
    
    let schedule =
        choose
            [ GET >=> choose
                [ pathScan "/schedule/%s" (fun s -> schedulePart s)
                  path "/schedule" >=> schedulePart "" ] ]
    
    let course =
        choose 
            [ GET >=> choose
                [ pathScan "/course/search/%s/%s" (fun (s, c) -> searchCourses s (Some c))
                  pathScan "/course/search/%s" (fun s -> searchCourses s None) ] ]
    
    let app =
        choose
            [ schedule
              course
              NOT_FOUND "Endpoint does not exists" ]
    
    [<EntryPoint>]
    let main args =
        startWebServer defaultConfig app
        0