#r @"./packages/FSharp.Data/lib/net40/FSharp.Data.DesignTime.dll"
#r @"./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r @"./build/Newtonsoft.Json.dll"
#r @"./build/Suave.dll"

#r @"./build/core.dll"
#r @"./build/api.exe"

open System.Net
open Suave
open Suave.Web

open Pilule.Api.app

let port = Sockets.Port.Parse "8083"
let serverConfig = { 
    defaultConfig with
        bindings = [ HttpBinding.mk HTTP IPAddress.Any port ] }

startWebServer serverConfig app

