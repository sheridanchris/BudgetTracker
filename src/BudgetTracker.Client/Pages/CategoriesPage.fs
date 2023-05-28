module CategoriesPage

open System
open Domain
open DomainLogic
open Remote
open Sutil
open Sutil.CoreElements
open Sutil.DaisyUI
open Sutil.Router
open ApplicationContext

type Model = {
  BudgetId: Guid
  SelectedCategoryName: string
  AllocationAmount: float
  ErrorMessage: string option
}

let categoryName model = model.SelectedCategoryName
let allocationAmount model = model.AllocationAmount
let errorMessage model = model.ErrorMessage

type Msg =
  | SetCategory of string
  | SetAllocation of float
  | Allocate
  | GotResponse of RpcResult<Budget>
  | GotException of exn

let initialState (budgetId: Guid) =
  {
    BudgetId = budgetId
    SelectedCategoryName = ""
    AllocationAmount = 0
    ErrorMessage = None
  },
  Cmd.none

let update msg model =
  match msg with
  | SetCategory categoryName -> { model with SelectedCategoryName = categoryName }, Cmd.none
  | SetAllocation allocation -> { model with AllocationAmount = allocation }, Cmd.none
  | Allocate ->
    model,
    Cmd.OfAsync.either
      Remoting.securedApi.AllocateCategory
      {
        BudgetId = model.BudgetId
        CategoryName = model.SelectedCategoryName
        Allocation = model.AllocationAmount
      }
      GotResponse
      GotException
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

        Daisy.Card.title [ Attr.text "Create Allocation" ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "select-category"
            Daisy.Label.labelText "Category"
          ]
          Daisy.Select.select [
            Attr.id "select-category"
            Bind.selected (model .> categoryName .> List.singleton, List.exactlyOne >> SetCategory >> dispatch)

            Daisy.Select.primary
            for category in budget.Categories |> List.map Category.name do
              Html.option [
                Attr.value category
                Attr.text category
              ]
          ]
        ]
        Daisy.FormControl.formControl [
          Daisy.Label.label [
            Attr.for' "allocation"
            Daisy.Label.labelText "Estimated Monthly Expense"
          ]
          Daisy.Input.input [
            Daisy.Input.bordered
            Attr.id "allocation"
            Attr.typeNumber
            Attr.min 0
            Attr.value (model .> allocationAmount, SetAllocation >> dispatch)
          ]
        ]
        Daisy.Card.actions [
          Attr.className "justify-end"

          Daisy.Button.button [
            Daisy.Button.primary
            Attr.text "Allocate"
            onClick (fun _ -> dispatch Allocate) []
          ]
        ]
      ]
    ]
  ]
