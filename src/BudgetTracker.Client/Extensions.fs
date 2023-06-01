[<AutoOpen>]
module Extensions

open Validus

[<RequireQualifiedAccess>]
module Bind =
  open System
  open Sutil
  open Sutil.Core

  let onlyIf (value: IObservable<bool>, element: SutilElement) =
    Bind.el (value, (fun value -> if value then element else Html.none))

  let optional (value: IObservable<'a option>, render: 'a -> SutilElement) =
    Bind.el (
      value,
      function
      | None -> Html.none
      | Some value -> render value
    )

[<RequireQualifiedAccess>]
module ValidationResult =
  let toMap validationResult =
    match validationResult with
    | Ok _ -> Map.empty
    | Error errors -> ValidationErrors.toMap errors
