module AllocatePage

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
  SelectedCategoryName: string option
  AllocationAmount: float
  ErrorMessage: string option
}

let categoryName model = model.SelectedCategoryName
let allocationAmount model = model.AllocationAmount
let errorMessage model = model.ErrorMessage

let selectedCategory model =
  match categoryName model with
  | None -> []
  | Some category -> List.singleton category

type Msg =
  | SetCategory of string option
  | SetAllocation of float
  | Allocate
  | GotResponse of RpcResult<Budget>
  | GotException of exn

let initialState (budget: Budget) =
  let selectedCategory = budget.Categories |> List.tryHead |> Option.map Category.name

  {
    BudgetId = budget.Id
    SelectedCategoryName = selectedCategory
    AllocationAmount = 0
    ErrorMessage = None
  },
  Cmd.none

let update msg model =
  match msg with
  | SetCategory categoryName -> { model with SelectedCategoryName = categoryName }, Cmd.none
  | SetAllocation allocation -> { model with AllocationAmount = allocation }, Cmd.none
  | Allocate ->
    let cmd =
      match model.SelectedCategoryName with
      | None -> Cmd.none
      | Some categoryName ->
        Cmd.OfAsync.either
          Remoting.securedApi.AllocateCategory
          {
            BudgetId = model.BudgetId
            CategoryName = categoryName
            Allocation = model.AllocationAmount
          }
          GotResponse
          GotException

    model, cmd
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
  let model, dispatch = budget |> Store.makeElmish initialState update ignore

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
          // TODO: Should this be a multi-select??? I think so.
          Daisy.Select.select [
            Daisy.Select.primary
            Attr.id "select-category"
            Bind.selected (model .> selectedCategory, List.tryExactlyOne >> SetCategory >> dispatch)
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
