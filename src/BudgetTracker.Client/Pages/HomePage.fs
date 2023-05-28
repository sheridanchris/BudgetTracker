module HomePage

open Sutil
open Sutil.CoreElements
open Sutil.DaisyUI
open ApplicationContext
open Domain
open Remote
open DomainLogic

let renderBudget (budget: Budget) =
  let totalExpenses = Budget.calculateTotalExpenses budget
  let isBudgetOverAllocated = Budget.isOverAllocated budget

  Daisy.Card.card [
    Daisy.Card.bordered
    Attr.className "shadow-md w-5/6 sm:w-4/6 md:w-3/6"

    Daisy.Card.body [
      Daisy.Card.title [
        Daisy.Link.link [
          Daisy.Link.hover
          Attr.text budget.Name
          Attr.href $"/#/budgets/{budget.Id}"
        ]
      ]

      match budget.Description with
      | None -> Html.none
      | Some description -> Html.p description

      Html.h2 [
        Attr.className (
          if isBudgetOverAllocated then
            "text-red-500"
          else
            "text-green-500"
        )
        Attr.text $"${totalExpenses} / ${budget.EstimatedMonthlyIncome}"
      ]
      Daisy.Progress.progress [
        Attr.min 0
        Attr.max budget.EstimatedMonthlyIncome
        Attr.value (min budget.EstimatedMonthlyIncome totalExpenses)
        Attr.className "w-100"
      ]
    ]
  ]

let noBudgetsFound () =
  text "Sorry, you don't have any budgets yet!"

let renderBudgets () =
  Html.div [
    Attr.className "w-full h-full"

    Html.a [
      Daisy.Button.buttonAttr
      Daisy.Button.small
      Attr.text "Create budget"
      Attr.href "/#/budgets/create"
    ]
    Bind.el (
      globalContext .> budgets,
      fun budgets ->
        if List.isEmpty budgets then
          noBudgetsFound ()
        else
          budgets
          |> List.map renderBudget
          |> Html.divc "flex flex-col gap-y-2.5 items-center"
    )
  ]

let renderUnauthenticatedView () =
  Html.div [
    Attr.className "h-full w-full items-center justify-center"
    text "Login to view and manage your budgets."
  ]

let view () =
  Bind.el (
    globalContext .> currentUser,
    fun currentUser ->
      match currentUser with
      | NotAuthenticated -> renderUnauthenticatedView ()
      | Authenticated _ -> renderBudgets ()
  )
