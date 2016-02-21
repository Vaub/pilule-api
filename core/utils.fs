namespace Pilule.Core

module Utils =

    let orElse otherwise original =
        match original with
        | Some v -> v
        | None -> otherwise

    module Strings =
    
        open System.Text.RegularExpressions

        let (|FirstRegexGroup|_|) pattern input =
            let m = Regex.Match(input,pattern) 
            if (m.Success) then Some m.Groups.[1].Value else None
            
        let (|ParseRegex|_|) regex str =
            let m = Regex(regex).Match(str)
            if m.Success
            then Some (List.tail [ for x in m.Groups -> x.Value ])
            else None
            
    module Computation = 
        
        type MaybeBuilder() =
            member this.Bind(x, f) =
                match x with
                | None -> None
                | Some v -> f v
            member this.Return(x) = Some x
        let maybe = new MaybeBuilder()