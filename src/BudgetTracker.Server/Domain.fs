module Domain

open System

// zero-based-budgeting - https://www.investopedia.com/terms/z/zbb.asp
// ability to create a MONTHLY budget with Estimated Monthly Income
// ability to create categories that can be used for each budget. (include average spent)
// ability to allocate $X to existing categories for your monthly budget.
// ability to create an expense in a specific budget category
// ability to view budget reports from previous months
// $ not allocated will go into savings, this can be allocated towards goals or spent.

// TODO: These budget functions are for the current month's budget, it gets reset every month... is that clear?
// TODO: Figure out how to model category & allocations per month

type Expense = {
  Amount: decimal
  Reason: string
  OccuredAt: DateTime
}

type AvailableCategory = { Name: string }

type AllocatedCategory = {
  Name: string
  Allocation: decimal
  Expenses: Expense list
}

type Category =
  | AvailableCategory of AvailableCategory
  | AllocatedCategory of AllocatedCategory

type Budget = {
  Id: Guid
  Name: string
  Description: string option
  EstimatedMonthlyIncome: decimal
  Categories: Category list
}

type TopCategory = {
  CategoryName: string
  TotalSpent: decimal
}

[<RequireQualifiedAccessAttribute>]
module Category =
  let defaultCategories =
    let categoryNames = [
      "Rent"
      "Groceries"
      "Food"
      "Vacations"
      "Vehicle"
    ]

    let createNewCategory name = AvailableCategory { Name = name }
    categoryNames |> List.map createNewCategory

  let name category =
    match category with
    | AvailableCategory category -> category.Name
    | AllocatedCategory category -> category.Name

  let chooseAllocated category =
    match category with
    | AvailableCategory _ -> None
    | AllocatedCategory category -> Some category

[<RequireQualifiedAccess>]
module AllocatedCategory =
  let sumExpenses allocatedCategory =
    allocatedCategory.Expenses |> List.sumBy (fun expense -> expense.Amount)

  let isOverSpent allocatedCategory =
    let totalExpenses = sumExpenses allocatedCategory
    totalExpenses > allocatedCategory.Allocation

  let createTopCategory allocatedCategory =
    let totalSpent = sumExpenses allocatedCategory

    {
      CategoryName = allocatedCategory.Name
      TotalSpent = totalSpent
    }

[<RequireQualifiedAccessAttribute>]
module Budget =
  let sumExpensesInBudget budget =
    budget.Categories
    |> List.choose Category.chooseAllocated
    |> List.sumBy AllocatedCategory.sumExpenses

  let doesCategoryExist categoryName budget =
    budget.Categories
    |> List.exists (fun category -> Category.name category = categoryName)

  let createCategory name budget =
    if doesCategoryExist name budget then
      Error "A category with that name already exists!"
    else
      let category = AvailableCategory { Name = name }
      Ok { budget with Categories = category :: budget.Categories }

  let createOrUpdateAllocatedCategory categoryName allocation budget =
    let createNewAllocatedCategory () =
      AllocatedCategory {
        Name = categoryName
        Allocation = allocation
        Expenses = []
      }

    let reallocateCategory allocatedCategory =
      AllocatedCategory { allocatedCategory with Allocation = allocation }

    let categories =
      if doesCategoryExist categoryName budget then
        budget.Categories
        |> List.map (fun category ->
          match category with
          | AvailableCategory category when category.Name = categoryName -> createNewAllocatedCategory ()
          | AllocatedCategory category when category.Name = categoryName -> reallocateCategory category
          | category -> category)
      else
        let category = createNewAllocatedCategory ()
        category :: budget.Categories

    { budget with Categories = categories }

  let topCategories amount budget =
    budget.Categories
    |> List.choose Category.chooseAllocated
    |> List.map AllocatedCategory.createTopCategory
    |> List.truncate amount

  let monthlyReset budget =
    let resetCategory category =
      AvailableCategory { Name = Category.name category }

    {
      budget with
          EstimatedMonthlyIncome = 0.00m
          Categories = budget.Categories |> List.map resetCategory
    }
