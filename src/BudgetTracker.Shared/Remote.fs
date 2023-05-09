module Remote

open System
open Domain

type CreateBudgetCommand = {
  Name: string
  Description: string option
  EstimatedMonthlyIncome: decimal
}

type CreateAllocatedCategoryCommand = {
  BudgetId: Guid
  CategoryName: string
  Allocation: decimal
}

type CreateExpenseCommand = {
  BudgetId: Guid
  CategoryName: string
  ExpenseAmount: decimal
  ExpenseDetails: string
}

type RpcResult<'a> =
  | Success of 'a
  | NoPermission
  | ErrorMessage of string

let errorMessage rpcResult =
  match rpcResult with
  | Success _ -> None
  | NoPermission -> Some "You don't have permission to perform that command."
  | ErrorMessage errorMessage -> Some errorMessage

type AsyncRpcResult<'a> = Async<RpcResult<'a>>

type SecuredApi = {
  GetBudgets: unit -> Async<Budget list>
  CreateBudget: CreateBudgetCommand -> AsyncRpcResult<Budget>
  AllocateCategory: CreateAllocatedCategoryCommand -> AsyncRpcResult<Budget>
  CreateExpense: CreateExpenseCommand -> AsyncRpcResult<Budget>
}
