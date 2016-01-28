namespace Pilule.Api

module Schedule =
    
    open System
    
    open Newtonsoft.Json
    open Suave
    open Suave.Json
    
    open Pilule.Core.Capsule
    open Pilule.Core.Utils.Strings
    
    let currentSemester =
        let now = DateTime.Now
        
        let year = now.Year
        let season = 
            match now.Month with
            | m when m < 5 -> Season.Winter
            | m when m < 9 -> Season.Summer
            | _ -> Season.Autumn
        { year = year; season = season }
    
    let extractSemester s =
        match s with
        | ParseRegex "([WwSsAa])([0-9]{4})" [season; year]
            -> 
            let semesterSeason =
                match season.ToLower() with
                | "w" -> Season.Winter
                | "s" -> Season.Summer
                | _ -> Season.Autumn
            { year = int year; season = semesterSeason }
        | _ -> currentSemester