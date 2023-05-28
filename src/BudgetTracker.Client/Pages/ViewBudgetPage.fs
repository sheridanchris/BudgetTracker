module ViewBudgetPage

open System
open Domain
open Sutil
open Sutil.DaisyUI
open ApplicationContext
open DomainLogic

let renderCategory (category: AllocatedCategory) =
  let totalExpenses = AllocatedCategory.sumExpenses category
  let isOverAllocated = AllocatedCategory.isOverAllocated category

  Daisy.Card.card [
    Daisy.Card.bordered
    Attr.className "shadow-md w-100"

    Daisy.Card.body [
      Daisy.Card.title [ Attr.text category.Name ]
      Html.h2 [
        Attr.className (if isOverAllocated then "text-red-500" else "text-green-500")
        Attr.text $"${totalExpenses} / ${category.Allocation}"
      ]
      Daisy.Progress.progress [
        Attr.min 0
        Attr.max category.Allocation
        Attr.value (min category.Allocation totalExpenses)
        Attr.className "w-80"
      ]
    ]
  ]

let renderNoCategoriesYet () =
  Html.div [
    Html.h1 [
      Attr.className "text-2xl"
      Attr.text "You haven't allocated any categories for this budget yet!"
    ]
  ]

let notFound () =
  text "A budget with that id was not found."

let view (budget: Budget) =
  let totalExpenses = Budget.calculateTotalExpenses budget
  let isOverAllocated = Budget.isOverAllocated budget
  let backgroundColor = if isOverAllocated then "bg-error" else "bg-primary"

  Html.div [
    Attr.className "flex flex-col w-full h-full items-center gap-y-2.5"
    Daisy.Stat.stats [
      Attr.className $"{backgroundColor} text-primary-content"
      Daisy.Stat.stat [
        Daisy.Stat.title [ Attr.text "Total Expenses" ]
        Daisy.Stat.value [ Attr.text $"${totalExpenses}" ]
      ]
      Daisy.Stat.stat [
        Daisy.Stat.title [ Attr.text "Estimated Monthly Income" ]
        Daisy.Stat.value [ Attr.text $"${budget.EstimatedMonthlyIncome}" ]
      ]
    ]

    let categories = budget.Categories |> List.choose Category.chooseAllocated

    if List.isEmpty categories then
      renderNoCategoriesYet ()
    else
      categories |> List.map renderCategory |> Html.div
  ]
