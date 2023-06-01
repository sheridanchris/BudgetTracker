module DomainLogic

open Domain

// zero-based-budgeting - https://www.investopedia.com/terms/z/zbb.asp
// ability to create a MONTHLY budget with Estimated Monthly Income
// ability to create categories that can be used for each budget. (include average spent)
// ability to allocate $X to existing categories for your monthly budget.
// ability to create an expense in a specific budget category
// ability to view budget reports from previous months
// $ not allocated will go into savings, this can be allocated towards goals or spent.

// TODO: These budget functions are for the current month's budget, it gets reset every month... is that clear?
// TODO: Figure out how to model category & allocations per month

// type TopCategory = {
//   CategoryName: string
//   TotalSpent: decimal
// }

[<RequireQualifiedAccess>]
module Category =
  let defaultCategories =
    let categoryNames = [
      "Rent"
      "Groceries"
      "Food"
      "Vacations"
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
  let create allocation (availableCategory: AvailableCategory) =
    AllocatedCategory {
      Name = availableCategory.Name
      Allocation = allocation
      Expenses = []
    }

[<RequireQualifiedAccess>]
module Budget =
  let create id ownerId name description estimatedMonthlyIncome = {
    Id = id
    OwnerId = ownerId
    Name = name
    Description = description
    EstimatedMonthlyIncome = estimatedMonthlyIncome
    Categories = Category.defaultCategories
  }

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
        // TODO: Should the category need to exist for allocations?
        // right now, the frontend uses a select element to pick a category... this doesn't give them the option to ...
        // create a new category.
        let category = createNewAllocatedCategory ()
        category :: budget.Categories

    { budget with Categories = categories }

  let createExpense categoryName amount reason nowUtc budget =
    // TODO: This tells me nothing about possible errors, "category does not exist."
    {
      budget with
          Categories =
            budget.Categories
            |> List.map (fun category ->
              match category with
              | AllocatedCategory category when category.Name = categoryName ->
                AllocatedCategory {
                  category with
                      Expenses =
                        {
                          Amount = amount
                          Reason = reason
                          OccuredAt = nowUtc
                        }
                        :: category.Expenses
                }
              | _ -> category)
    }

  let monthlyReset budget =
    let resetCategory category =
      AvailableCategory { Name = Category.name category }

    {
      budget with
          EstimatedMonthlyIncome = 0.00
          Categories = budget.Categories |> List.map resetCategory
    }
