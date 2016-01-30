namespace Pilule.Core

module Course =
    
    open FSharp.Data
    
    open Capsule
    open Requests
    open Utils
    
    [<Literal>]
    let courseEndpoint = "/pls/etprod8/bwskfcls.P_GetCrse_Advanced"
    
    let defaultSubjects = seq [|"ACT";"AEE";"ADM";"ADS";"APR";"AGC";"AGF";"AGN";"ALL";"AME";"ANM";"ANG";"ANL";"ANT";"ARA";"ARL";"ARC";"GAD";"ARD";"ANI";"ART";"ARV";"ASR";"BCM";"BCX";"BIF";"BIO";"BMO";"BVG";"BPH";"CAT";"CHM";"CHN";"CIN";"COM";"CTB";"CNS";"CSO";"CRL";"CRI";"DES";"DDU";"DVE";"DRI";"DID";"DRT";"ERU";"ECN";"EDC";"EPS";"ENP";"ENS";"EER";"ENT";"ENV";"EPM";"EGN";"ERG";"ESP";"ESG";"ETH";"EFN";"ETN";"EAN";"ETI";"PTR";"GPL";"EXD";"FOR";"FIS";"FPT";"FRN";"FLE";"FLS";"GAA";"GAE";"GAL";"GCH";"GCI";"GPG";"GEX";"GEL";"GSC";"GGL";"GIN";"GIF";"GLO";"GMC";"GML";"GMN";"GPH";"GGR";"GLG";"GMT";"GSO";"GRH";"GSE";"GSF";"GIE";"GUI";"GRC";"HST";"HAR";"IFT";"IED";"ITL";"JAP";"KIN";"LMO";"LOA";"LAT";"LNG";"LIT";"MNG";"MRK";"MAT";"MED";"MDD";"MDX";"MEV";"MQT";"MET";"MCB";"MSL";"MUS";"NRB";"NUT";"OCE";"OPV";"ORT";"PST";"PUN";"PHA";"PHC";"PHI";"PHS";"PHT";"PHY";"PLG";"PFP";"POR";"PSA";"PSE";"PSY";"PPG";"RLT";"RUS";"SAT";"SAC";"POL";"SAN";"SBM";"SCR";"SBO";"SIN";"STC";"STA";"SVS";"SEX";"SOC";"SLS";"STT";"SIO";"TEN";"THT";"THL";"TCF";"TRE";"TXM";"TRD";"TED"|]
    
    type SearchParam = {
        semester: Semester
        mode: SearchMode
    }
    and SearchMode =
        | Sign of CourseSubject * string
        | Title of seq<CourseSubject> * string
    and CourseSubject = string
    
    let internal createSearchRequest formData sessionToken =
        fun unit ->
            Http.Request (
                url = (Requests.host + courseEndpoint),
                httpMethod = HttpMethod.Post,
                body = FormValues formData,
                cookies = [("SESSID", sessionToken)]
            )
    
    let internal parseSearchResults (n: HtmlDocument) =
        let parseRow (r: HtmlNode) =
            let columns = r.Descendants ["td"] |> Seq.map (fun c -> c.InnerText().Trim())
            
            let extractCourseId subject number =
                { subject = subject; number = number }

            { 
                nrc = int (Seq.item 1 columns)
                id = extractCourseId (Seq.item 2 columns) (Seq.item 3 columns)
                name = columns |> Seq.item 7
                category = columns |> Seq.item 4
                teacher = columns |> Seq.item 19
                credits = columns |> Seq.item 6  
            }
            
    
        let table =
            n.Descendants ["table"]
            |> Seq.filter (fun t -> t.HasClass "datadisplaytable")
            |> Seq.item 0
            
        table.Descendants ["tr"]
        |> Seq.filter (fun r -> 
            (r.Descendants ["th"] |> Seq.isEmpty) && 
            (r.Descendants ["td"] |> Seq.item 0).InnerText().Trim() <> "")
        |> Seq.map (fun r -> parseRow r)
    
    let findCourses searchParam sessionToken =
        
        let session =
            match sessionToken with
            | Some t -> t
            | None -> failwith "Could not find session token!"
        
        let subjects = 
            match searchParam.mode with
            | Sign (subject, _) -> seq [("sel_subj", subject)]
            | Title (subjects, _) ->
                let subjectsToAdd = 
                    if not (Seq.isEmpty subjects) then 
                        subjects |> Seq.map (fun s -> s.ToUpper())
                    else 
                        defaultSubjects
                seq { for s in subjects do yield ("sel_subj", s) }
        
        let title =
            match searchParam.mode with
            | Sign (_, num) -> ""
            | Title (_, title) -> title
        
        let courseNb =
            match searchParam.mode with
            | Sign (_, num) -> num.ToString()
            | _ -> ""
        
        let capsuleParams =
            Seq.append (
                seq [
                    ("term_in", Semester.toCapsuleFormat searchParam.semester)
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
                    ("sel_crse", courseNb)
                    ("sel_title", title)
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
                    ("SUB_BTN","Recherche de groupe")
            ]) <| subjects
        
        let request = createSearchRequest capsuleParams session
        match runRequest request with
        | Response r -> 
            let body = extractBody r.Body
            match body with
            | Some b when b.Contains "Aucun cours ne correspond à vos critères de recherche" ->
                seq []
            | Some b ->
                parseSearchResults (HtmlDocument.Parse b)
            | None ->
                seq []
        | Error -> seq []