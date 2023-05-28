module Domain

open System

// TODO: Decimal comparison seems to be broken in Fable, create repro and file an issue.

type Expense = {
  Amount: float
  Reason: string
  OccuredAt: DateTime
}

type AvailableCategory = { Name: string }

type AllocatedCategory = {
  Name: string
  Allocation: float
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
  EstimatedMonthlyIncome: float
  Categories: Category list
}
