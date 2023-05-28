module Remoting

open Fable.Remoting.Client

let publicApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<Remote.PublicApi>

let securedApi =
  Remoting.createApi ()
  |> Remoting.withBaseUrl "/api"
  |> Remoting.buildProxy<Remote.SecuredApi>
