namespace Pilule.Core

module Schedule =
    
    open System
    open FSharp.Data
    
    open Capsule
    open Requests
    open Utils
    open Utils.Strings
    
    let scheduleEndpoint = "/pls/etprod8/bwskfshd.P_CrseSchdDetl"
    
    let internal isACourseInfoNode (node: HtmlNode) =
        let captions =
            node.Descendants ["caption"]
            |> Seq.filter (fun n -> (extractInnerText n) <> "Horaires prévus")
        not (Seq.isEmpty captions)
    
    let internal isACourseScheduleNode (node: HtmlNode) =
        let captions =
            node.Descendants ["caption"]
            |> Seq.filter (fun n -> (extractInnerText n) = "Horaires prévus")
        not (Seq.isEmpty captions)
    
    let internal extractCourseDetail name (rows: seq<HtmlNode>) =
        let checkDetailNode (n: HtmlNode) =
            (n.Descendants ["th"] |> Seq.head |> extractInnerText).Contains name
        
        let extractDetail (n: HtmlNode) =
            (n.Descendants ["td"] |> Seq.head |> extractInnerText)
        
        rows
        |> Seq.filter (fun r -> checkDetailNode r)
        |> Seq.tryHead
        |> Option.map (fun r -> extractDetail r)
    
    let internal extractCourseInfo (node: HtmlNode) =
        let caption = node.Descendants ["caption"] |> Seq.head
        let rows = node.Descendants ["tr"]
        
        let (name, id, category) =
            match caption.InnerText() with
            | ParseRegex "(.*) - ([A-Z]{3,4}) ([A-Z0-9]{3,5}) - (.*)" [name; subject; number; category]
                -> (name, { subject = subject; number = number }, category)
            | _ -> ("", { subject = ""; number = "" }, "")
        
        let nrc = 
            extractCourseDetail "NRC:" rows
            |> Option.map (fun n -> int n)
            |> orElse 0
        
        let credits =
            extractCourseDetail "Crédits:" rows |> orElse ""
        
        let teacher =
            extractCourseDetail "Professeur:" rows |> orElse ""
        
        { nrc = nrc; id = id; name = name; category = category; credits = credits; teacher = teacher; }
    
    let internal extractCourseSchedule (node: HtmlNode) =
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
                | _ -> (TimeSpan(-1,0,0), TimeSpan(-1,0,0))
            let day =
                match dayNode.InnerText().Trim().ToLower() with
                | "l" -> DayOfWeek.Monday
                | "m" -> DayOfWeek.Tuesday
                | "r" -> DayOfWeek.Wednesday
                | "j" -> DayOfWeek.Thursday
                | "v" -> DayOfWeek.Friday
                | _ -> DayOfWeek.Sunday
            
            { fromHour = (fst time); toHour = (snd time); day = day }
        
        let extractDuration (n: HtmlNode): CourseDuration =
            let (fromDate, toDate) =
                match n.InnerText().Trim() with
                | ParseRegex "([0-9]{4}\/[0-9]{2}\/[0-9]{2}) - ([0-9]{4}\/[0-9]{2}\/[0-9]{2})" [fromText; toText]
                    -> (DateTime.ParseExact(fromText, "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture),
                        DateTime.ParseExact(toText, "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture))
                | _ -> (DateTime.MinValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime())
        
            { fromDate = fromDate; toDate = toDate }
        
        let extractCourseTimes (n: HtmlNode) =
            let columns = n.Descendants ["td"]
            let (room, time, day, duration) = (
                Seq.item 3 columns, 
                Seq.item 1 columns, 
                Seq.item 2 columns,
                Seq.item 4 columns )
            { room = extractRoom room; time = extractTime time day; duration = extractDuration duration }
            
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
                yield { course = courseNodes |> Seq.item i; schedule = scheduleNode |> Seq.item i  }
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
                    
        match runRequest scheduleRequest with
        | Response r -> 
            extractBody r.Body
            |> Option.map (fun b -> parseSchedule b) 
            |> orElse Seq.empty
        | _ -> Seq.empty