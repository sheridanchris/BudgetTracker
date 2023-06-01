module CreateCategoryPage

open System
open Domain
open Remote
open Sutil
open Sutil.CoreElements
open ApplicationContext
open Sutil.DaisyUI
open Sutil.Router

type Model = {
  CreateCategoryCommand: CreateCategoryCommand
  ErrorMessage: string option
}

type Msg =
  | SetCategoryName of string
  | CreateCategory
  | GotResponse of RpcResult<Budget>
  | GotException of exn

let categoryName model =
  model.CreateCategoryCommand.CategoryName

let errorMessage model = model.ErrorMessage

let initialState (budgetId: Guid) =
  {
    CreateCategoryCommand = {
      CategoryName = ""
      BudgetId = budgetId
    }
    ErrorMessage = None
  },
  Cmd.none

let update msg model =
  match msg with
  | SetCategoryName categoryName ->
    { model with CreateCategoryCommand = { model.CreateCategoryCommand with CategoryName = categoryName } }, Cmd.none
  | CreateCategory ->
    model, Cmd.OfAsync.either Remoting.securedApi.CreateCategory model.CreateCategoryCommand GotResponse GotException
  | GotResponse(Success budget) ->
    model,
    Cmd.batch [
      Cmd.ofEffect (fun _ -> globalDispatch (UpdateBudget budget))
      Router.navigate $"/#/budgets/{budget.Id}"
    ]
  | GotResponse response -> { model with ErrorMessage = RpcResult.errorMessage response }, Cmd.none
  | GotException _ ->
    { model with ErrorMessage = Some "Something went wrong with that request! Please try again." }, Cmd.none

let view (budget: Budget) =
  let model, dispatch = budget.Id |> Store.makeElmish initialState update ignore

  Html.div [
    disposeOnUnmount [ model ]
    Attr.className "flex flex-col justify-center items-center h-full w-full"

    Daisy.Card.card [
      Daisy.Card.bordered
      Attr.className "shadow-md"

      Daisy.Card.body [
        Bind.optional (
          model .> errorMessage,
          fun error ->
            Daisy.Alert.alert [
              Daisy.Alert.error
              Attr.text error
            ]
        )

        Daisy.Card.title [ Attr.text "Create Category" ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "category-name"
            Daisy.Label.labelText "Category Name"
          ]
          Daisy.Input.input [
            Daisy.Input.bordered
            Attr.id "category-name"
            Attr.placeholder "category name"
            Attr.value (model .> categoryName, SetCategoryName >> dispatch)
          ]
        ]
        Daisy.Card.actions [
          Attr.className "justify-end"
          Daisy.Button.button [
            Daisy.Button.primary
            Attr.text "Create"
            onClick (fun _ -> dispatch CreateCategory) []
          ]
        ]
      ]
    ]
  ]
