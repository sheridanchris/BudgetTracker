module CreateBudgetPage

open System
open Sutil
open Sutil.CoreElements
open Sutil.DaisyUI
open Remote
open Domain
open ApplicationContext
open Sutil.Router
open Validus

type Model = {
  Command: CreateBudgetCommand
  ErrorMessage: string option
  ValidationErrors: Map<string, string list>
}

type Msg =
  | SetName of string
  | SetDescription of string option
  | SetEstimatedMonthlyIncome of float
  | CreateBudget
  | GotResponse of RpcResult<Budget>
  | GotException of exn

let name model = model.Command.Name
let description model = model.Command.Description
let estimatedMonthlyIncome model = model.Command.EstimatedMonthlyIncome
let errorMessage model = model.ErrorMessage
let defaultEmptyString = Option.defaultValue ""

let errors key model =
  model.ValidationErrors |> Map.tryFind key |> Option.defaultValue []

let nameErrors model =
  errors (nameof model.Command.Name) model

let descriptionErrors model =
  errors (nameof model.Command.Description) model

let incomeErrors model =
  errors (nameof model.Command.EstimatedMonthlyIncome) model

let descriptionFromInput description =
  if String.IsNullOrWhiteSpace description then
    None
  else
    Some description

let initialState () =
  {
    Command = {
      Name = ""
      Description = None
      EstimatedMonthlyIncome = 1000.0
    }
    ErrorMessage = None
    ValidationErrors = Map.empty
  },
  Cmd.none

let updateCommand command model =
  {
    model with
        Command = command
        ValidationErrors =
          match command.Validate() with
          | Ok _ -> Map.empty
          | Error errors -> ValidationErrors.toMap errors
  },
  Cmd.none

let update msg model =
  match msg with
  | SetName name -> updateCommand { model.Command with Name = name } model
  | SetDescription description -> updateCommand { model.Command with Description = description } model
  | SetEstimatedMonthlyIncome income -> updateCommand { model.Command with EstimatedMonthlyIncome = income } model
  | CreateBudget -> model, Cmd.OfAsync.either Remoting.securedApi.CreateBudget model.Command GotResponse GotException
  | GotResponse(Success budget) ->
    model,
    Cmd.batch [
      Cmd.ofEffect (fun _ -> globalDispatch (AddBudget budget))
      Router.navigate "/#/"
    ]
  | GotResponse response -> { model with ErrorMessage = RpcResult.errorMessage response }, Cmd.none
  | GotException _ ->
    { model with ErrorMessage = Some "Something went wrong with that request! Please try again." }, Cmd.none

let view () =
  let model, dispatch = () |> Store.makeElmish initialState update ignore

  Html.div [
    disposeOnUnmount [ model ]
    Attr.className "flex justify-center items-center h-full w-full"

    Daisy.Card.card [
      Daisy.Card.bordered
      Attr.className "shadow-xl w-100"

      Daisy.Card.body [
        Bind.optional (
          model .> errorMessage,
          fun error ->
            Daisy.Alert.alert [
              Daisy.Alert.error
              Attr.text error
            ]
        )

        Daisy.Card.title [ Attr.text "Create a Budget" ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "budget-name"
            Daisy.Label.labelText "Name"
          ]
          Components.validatedDaisyInput (
            model .> name,
            SetName >> dispatch,
            model .> nameErrors,
            [
              Attr.id "budget-name"
              Attr.placeholder "enter a name"
            ]
          )
        ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "budget-description"
            Daisy.Label.labelText "Description"
          ]
          Components.validatedDaisyInput (
            model .> description .> defaultEmptyString,
            descriptionFromInput >> SetDescription >> dispatch,
            model .> descriptionErrors,
            [
              Attr.id "budget-description"
              Attr.placeholder "enter a description"
            ]
          )
        ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "budget-allocation"
            Daisy.Label.labelText "Estimated Monthly Income"
          ]
          Components.validatedDaisyInput (
            model .> estimatedMonthlyIncome,
            SetEstimatedMonthlyIncome >> dispatch,
            model .> incomeErrors,
            [
              Attr.id "budget-allocation"
              Attr.placeholder "enter an amount"
              Attr.typeNumber
              Attr.min 0
            ]
          )
        ]

        Daisy.Card.actions [
          Attr.className "justify-end"
          Daisy.Button.button [
            Daisy.Button.small
            Daisy.Button.primary
            Attr.text "Create budget"
            onClick (fun _ -> dispatch CreateBudget) []
          ]
        ]
      ]
    ]
  ]
