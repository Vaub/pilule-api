namespace Pilule.Core.Utils

module Strings =
  
    open System.Text.RegularExpressions

    let (|FirstRegexGroup|_|) pattern input =
        let m = Regex.Match(input,pattern) 
        if (m.Success) then Some m.Groups.[1].Value else None