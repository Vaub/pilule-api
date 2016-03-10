namespace Pilule.Core
    
    open FSharp.Data
    open FSharp.Collections.ParallelSeq
    
    open Capsule
    open Utils.Strings
    
    module Course =
    
        type Course = {
            Nrc: Nrc
            Sign: CourseSign
            Name: string
            Category: CourseCategory
            Teacher: CourseTeacher
            Credits: CourseCredits
        }
        and CourseSign = {
            Subject: CourseSubject
            Number: CourseNumber
        } with
            override x.ToString () = sprintf "%s-%s" x.Subject x.Number 
        and Nrc = int
        and CourseSubject = string
        and CourseNumber = string
        and CourseCategory = string
        and CourseTeacher = string
        and CourseCredits = string
        
        type SearchParameters = {
            Semester: Semester
            Mode: SearchMode
        }
        and SearchMode =
            | BySign of CourseSign
            | ByName of NameSearch * seq<CourseSubject> with
            member x.ListSubjects () =
                match x with
                | BySign s -> seq [s.Subject]
                | ByName (n, s) -> s
        and NameSearch = string
        
        type CourseFetcher = 
            SearchParameters -> RequestConfiguration -> Async<HtmlDocument option>
        and CourseParser = 
            HtmlDocument -> Course seq
            
        type CourseFinder = {
            Fetcher: CourseFetcher
            Parser: CourseParser
        } with
            member x.FindCoursesAsync s c = 
                async {
                    let! document = x.Fetcher s c
                    return
                        match document with
                        | Some d -> x.Parser d
                        | None -> seq []
                }
            member x.FindCourses s c =
                x.FindCoursesAsync s c |> Async.RunSynchronously
            
        
        module Parser = 
            
            open Utils.Computation
            
            let internal parseCourseNode (r: HtmlNode) =
                let columns = 
                    r.Descendants ["td"] 
                    |> Seq.map (fun c -> c.InnerText().Trim())
                
                maybe {
                    let! nrc =          columns |> Seq.tryItem 1
                    let! signSubject =  columns |> Seq.tryItem 2
                    let! signNumber =   columns |> Seq.tryItem 3
                    let! name =         columns |> Seq.tryItem 7
                    let! category =     columns |> Seq.tryItem 4
                    let! teacher =      columns |> Seq.tryItem 19
                    let! credits =      columns |> Seq.tryItem 6
                    
                    return {
                        Nrc = int nrc
                        Sign = { Subject = signSubject; Number = signNumber }
                        Name = name
                        Category = category
                        Teacher = teacher
                        Credits = credits
                    }
                }
            
            let internal findCourseTable (d: HtmlDocument) =
                d.Descendants ["table"]
                |> Seq.filter (fun r -> r.HasClass "datadisplaytable")
                |> Seq.tryItem 0
            
            let inline internal isNodeACourseRow (n: HtmlNode) =
                (n.Descendants ["th"] |> Seq.isEmpty)
                && 
                match n.Descendants ["td"] |> Seq.tryItem 0 with
                    | Some c -> c.InnerText().Trim() <> ""
                    | None -> false
            
            let internal findCourseRows (n: HtmlNode) =
                n.Descendants ["tr"]
                |> Seq.filter (fun r -> isNodeACourseRow r)
            
            let capsuleParser input: Course seq = 
                match findCourseTable input with
                | Some n ->
                    findCourseRows n
                    |> PSeq.map (fun c -> parseCourseNode c)
                    |> Seq.choose id
                | None -> Seq.empty
                
            
        module Fetcher =
            
            open Requests
            
            [<Literal>]
            let endpoint = "/pls/etprod8/bwskfcls.P_GetCrse_Advanced"
            
            let internal createRequestParameters p =
                let courseSubjects =
                    p.Mode.ListSubjects ()
                    |> Seq.map (fun s -> ("sel_subj", s))
                let nameToSearch =
                    match p.Mode with
                    | ByName (n, _) -> n
                    | _ -> ""
                let courseNumber = 
                    match p.Mode with
                    | BySign s -> s.Number
                    | _ -> ""
                
                Seq.append
                <| seq [("term_in", Semester.toCapsuleFormat p.Semester)
                        ("rsts","dummy")
                        ("crn","dummy")
                        ("sel_subj","dummy")
                        ("sel_day","dummy")
                        ("sel_schd","dummy")
                        ("sel_insm","dummy")
                        ("sel_camp","dummy")
                        ("sel_levl","dummy")
                        ("sel_sess","dummy")
                        ("sel_instr","dummy")
                        ("sel_ptrm","dummy")
                        ("sel_attr","dummy")
                        ("sel_crse", courseNumber)
                        ("sel_title", nameToSearch)
                        ("sel_schd","%")
                        ("sel_from_cred","")
                        ("sel_to_cred","")
                        ("sel_camp","%")
                        ("sel_levl","%")
                        ("sel_ptrm","%")
                        ("sel_dunt_unit","")
                        ("sel_dunt_code","AN")
                        ("call_value_in","")
                        ("sel_instr","%")
                        ("sel_sess","%")
                        ("sel_attr","%")
                        ("begin_hh","0")
                        ("begin_mi","0")
                        ("begin_ap","x")
                        ("end_hh","0")
                        ("end_mi","0")
                        ("end_ap","x")
                        ("path","1")
                        ("SUB_BTN","Recherche de groupe") ]
                <| courseSubjects
            
            let internal fetchCourseDocument p c =
                fun () ->
                    Http.AsyncRequest (
                        url = (c.Host + endpoint),
                        httpMethod = HttpMethod.Post,
                        body = FormValues p,
                        cookies = [("SESSID", c.SessionToken)]
                    )
            
            let capsuleAsyncFetcher searchParameters config = 
                let parameters = searchParameters |> createRequestParameters
                async {
                    let request = fetchCourseDocument parameters config
                    let! response = request |> runRequestAsync
                    return extractResponseBody response      
                }
                