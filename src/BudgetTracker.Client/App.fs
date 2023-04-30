open Sutil
open Sutil.CoreElements
open Sutil.DaisyUI
open Fable.Core.JsInterop

let increment number = number + 1
let decrement number = number - 1

let view () =
  let number = Store.make 0

  Html.div [
    disposeOnUnmount [ number ]

    Bind.el (number, Html.p)

    Daisy.Button.button [
      Daisy.Button.primary
      Daisy.Button.small
      Attr.text "Increment"
      onClick (fun _ -> Store.modify increment number) []
    ]
    Daisy.Button.button [
      Daisy.Button.primary
      Daisy.Button.small
      Attr.text "Decrement"
      onClick (fun _ -> Store.modify decrement number) []
    ]
  ]

importSideEffects "./styles.css"
Program.mount ("sutil-app", view ()) |> ignore
