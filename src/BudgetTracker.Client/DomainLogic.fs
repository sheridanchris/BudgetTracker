module DomainLogic

open Domain

module Category =
  let chooseAllocated category =
    match category with
    | AvailableCategory _ -> None
    | AllocatedCategory category -> Some category

  let name category =
    match category with
    | AvailableCategory available -> available.Name
    | AllocatedCategory allocated -> allocated.Name

module AllocatedCategory =
  let sumExpenses category =
    category.Expenses |> List.sumBy (fun expense -> expense.Amount)

  let isOverAllocated category =
    sumExpenses category > category.Allocation

module Budget =
  let calculateTotalExpenses budget =
    budget.Categories
    |> List.choose Category.chooseAllocated
    |> List.map AllocatedCategory.sumExpenses
    |> List.sum

  let isOverAllocated budget =
    calculateTotalExpenses budget > budget.EstimatedMonthlyIncome
