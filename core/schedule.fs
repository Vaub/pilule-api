namespace Pilule.Core

module Schedule =
    
    open System
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
    
    let isACourseScheduleNode (node: HtmlNode) =
        let captions =
            node.Descendants ["caption"]
            |> Seq.filter (fun n -> n.InnerText() = "Horaires prévus")
        not (Seq.isEmpty captions)
    
    let extractCourseInfo (node: HtmlNode) =
        let caption = node.Descendants ["caption"] |> Seq.head
        let rows = node.Descendants ["tr"]
        
        let title =
            match caption.InnerText() with
            | ParseRegex "(.*) - ([A-Z]{3,4} [A-Z0-9]{3,5}) - (.*)" [n; s; t]
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
    
    let extractCourseSchedule (node: HtmlNode) =
        let rows = node.Descendants ["tr"] |> Seq.skip 1
        
        let extractRoom (n: HtmlNode) =
            match n.InnerText() with
            | t when t.Contains "ACU" -> NoRoom
            | t -> Room t
        
        let extractTime (timeNode: HtmlNode) (dayNode: HtmlNode): CourseTime =
            let time =
                match timeNode.InnerText().Trim() with
                | ParseRegex "([0-9]{1,2}):([0-9]{1,2}) - ([0-9]{1,2}):([0-9]{1,2})" [h1; m1; h2; m2]
                    -> (TimeSpan(int h1, int m1, 0), TimeSpan(int h2, int m2, 0))
                | _ -> (TimeSpan.MinValue, TimeSpan.MinValue)
            let day =
                match dayNode.InnerText().Trim().ToLower() with
                | "l" -> DayOfWeek.Monday
                | "m" -> DayOfWeek.Tuesday
                | "r" -> DayOfWeek.Wednesday
                | "j" -> DayOfWeek.Thursday
                | "v" -> DayOfWeek.Friday
                | _ -> DayOfWeek.Sunday
            
            { fromHour = (fst time); toHour = (snd time); day = day }
            
        let extractCourseTimes (n: HtmlNode) =
            let columns = n.Descendants ["td"]
            let (room, time, day) = (Seq.item 3 columns, Seq.item 1 columns, Seq.item 2 columns)
            { room = extractRoom room; time = (extractTime time day) }
            
        rows |> Seq.map (fun r -> extractCourseTimes r)
    
    let parseSchedule body =
        let tables = (HtmlDocument.Parse body).Descendants ["table"]
        
        let courseNodes =
            tables
            |> Seq.filter (fun n -> isACourseInfoNode n)
            |> Seq.map (fun n -> extractCourseInfo n)
        
        let scheduleNode =
            tables
            |> Seq.filter (fun n -> isACourseScheduleNode n)
            |> Seq.map (fun n -> extractCourseSchedule n)
        
        seq {
            for i = 0 to (Seq.length courseNodes) - 1 do
                yield { Seq.item i courseNodes with schedule = Seq.item i scheduleNode }
        }
    
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