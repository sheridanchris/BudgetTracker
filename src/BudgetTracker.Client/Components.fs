module Components

open Sutil
open Sutil.Core
open Sutil.CoreElements
open Sutil.DaisyUI
open System

let validatedDaisyInput
  (
    value: IObservable<'value>,
    onValueChanged: 'value -> unit,
    validationErrors: IObservable<string list>,
    elements: seq<SutilElement>
  ) =
  // Only display the validation error if the value has been changed from its default.
  let hasValueBeenChanged = Store.make false

  fragment [
    disposeOnUnmount [ hasValueBeenChanged ]

    Daisy.Input.input [
      Bind.onlyIf (
        hasValueBeenChanged,
        Bind.toggleClass (validationErrors .> List.isEmpty, "input-success", "input-error")
      )
      Attr.value (
        value,
        fun value ->
          onValueChanged value
          Store.set hasValueBeenChanged true
      )
      yield! elements
    ]
    Bind.onlyIf (
      hasValueBeenChanged,
      Bind.optional (
        validationErrors .> List.tryHead,
        fun validationError ->
          Html.p [
            Attr.className "text-red-500"
            Attr.text validationError
          ]
      )
    )
  ]
