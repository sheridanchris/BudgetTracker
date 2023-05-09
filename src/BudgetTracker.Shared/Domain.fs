module Domain

open System

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
  OwnerId: string
  Name: string
  Description: string option
  EstimatedMonthlyIncome: decimal
  Categories: Category list
}
