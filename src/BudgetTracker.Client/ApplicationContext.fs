module ApplicationContext

open Sutil
open Domain
open Remote

type Context = {
  User: CurrentUser
  Budgets: Budget list
}

let currentUser model = model.User
let budgets model = model.Budgets

type ContextMsg =
  | SetBudgets of Budget list
  | SetCurrentUser of CurrentUser
  | AddBudget of Budget
  | UpdateBudget of Budget

let private init () =
  {
    User = NotAuthenticated
    Budgets = []
  },
  Cmd.OfAsync.perform Remoting.publicApi.GetUser () SetCurrentUser

let private update msg model =
  match msg with
  | SetBudgets budgets -> { model with Budgets = budgets }, Cmd.none
  | AddBudget budget -> { model with Budgets = budget :: model.Budgets }, Cmd.none
  | SetCurrentUser NotAuthenticated ->
    {
      model with
          User = NotAuthenticated
          Budgets = []
    },
    Cmd.none
  | SetCurrentUser(Authenticated userModel) ->
    { model with User = Authenticated userModel }, Cmd.OfAsync.perform Remoting.securedApi.GetBudgets () SetBudgets
  | UpdateBudget newBudget ->
    {
      model with
          Budgets =
            model.Budgets
            |> List.map (fun budget -> if budget.Id = newBudget.Id then newBudget else budget)
    },
    Cmd.none

let globalContext, globalDispatch = () |> Store.makeElmish init update ignore
