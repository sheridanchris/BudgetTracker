module Components

open Sutil
open Sutil.Core
open Sutil.CoreElements
open Sutil.DaisyUI
open System

let inline validationComponent
  (
    value: IObservable<'value>,
    onValueChanged: 'value -> unit,
    validationErrors: IObservable<string list>,
    elementFactory: seq<SutilElement> -> SutilElement,
    successClassName: string,
    errorClassName: string,
    elements: seq<SutilElement>
  ) =
  fragment [
    elementFactory [
      Bind.toggleClass (validationErrors .> List.isEmpty, successClassName, errorClassName)
      Attr.value (value, onValueChanged)
      yield! elements
    ]
    Bind.optional (
      validationErrors .> List.tryHead,
      fun validationError ->
        Html.p [
          Attr.className "text-red-500"
          Attr.text validationError
        ]
    )
  ]

// TODO: This may not need to be _this_ generic.
let inline validatedDaisyInput
  (
    value: IObservable<'value>,
    onValueChanged: 'value -> unit,
    validationErrors: IObservable<string list>,
    inputElements: seq<SutilElement>
  ) =
  validationComponent (
    value,
    onValueChanged,
    validationErrors,
    Daisy.Input.input,
    "input-success",
    "input-error",
    inputElements
  )
