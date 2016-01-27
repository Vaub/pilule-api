namespace Pilule.Core

module Schedule =
    
    open FSharp.Data
    
    open Capsule
    open Requests
    open Utils
    open Utils.Strings
    
    let scheduleEndpoint = "/pls/etprod8/bwskfshd.P_CrseSchdDetl"
    
    let isACourseInfoNode (node: HtmlNode) =
        let captions =
            node.Descendants ["caption"]
            |> Seq.filter (fun n -> n.InnerText() <> "Horaires prévus")
        not (Seq.isEmpty captions)
    
    let extractCourseInfo (node: HtmlNode) =
        let caption = node.Descendants ["caption"] |> Seq.head
        let rows = node.Descendants ["tr"]
        
        let title =
            match caption.InnerText() with
            | ParseRegex "(.*) - ([A-Z]{3,4} [0-9]{3,5}) - (.*)" [n; s; t]
                -> (n, s)
            | _ -> ("", "")
            
        let teacher =
            rows 
            |> Seq.filter (fun r ->
                (r.Descendants ["th"] |> Seq.head).InnerText().Contains "Professeur:")
            |> Seq.tryHead
            |> Option.map (fun r -> (r.Descendants ["td"] |> Seq.head).InnerText().Trim())
            |> orElse ""
        
        { sign = (snd title); name = (fst title); teacher = teacher; schedule = []  }
    
    let parseSchedule body =
        let document = HtmlDocument.Parse body
        let courseNodes =
            document.Descendants ["table"]
            |> Seq.filter (fun n -> isACourseInfoNode n)
            |> Seq.map (fun n -> extractCourseInfo n)
            
        courseNodes
    
    let findSchedule semester session: Schedule =
        let semesterParam = Semester.toCapsuleFormat semester
        let scheduleUrl = (Requests.host + scheduleEndpoint)
        
        let sessid =
            match session with
            | Some s -> s
            | _ -> failwith "Could not find session cookie"
        
        let scheduleRequest = 
            fun unit ->
                Http.Request ( 
                    url = scheduleUrl, httpMethod = HttpMethod.Post,
                    body = FormValues [("term_in", semesterParam)],
                    cookies = [("SESSID", sessid)])
        let courses = 
            match runRequest scheduleRequest with
            | Response r -> 
                extractBody r.Body 
                |> Option.map (fun b -> parseSchedule b) 
                |> orElse Seq.empty
            | _ -> Seq.empty
        
        { semester = semester; courses = courses }