module Remote

open System
open Domain
open Validus

type CreateBudgetCommand = {
  Name: string
  Description: string option
  EstimatedMonthlyIncome: float
} with

  member this.Validate() =
    validate {
      let! _ = Check.WithMessage.String.notEmpty (sprintf "%s must not be empty.") (nameof this.Name) this.Name

      let! _ =
        Check.optional
          (Check.WithMessage.String.notEmpty (sprintf "%s must not be empty."))
          (nameof this.Description)
          this.Description

      let! _ =
        Check.WithMessage.Float.greaterThan
          0.
          (sprintf "%s must be greater than $0.")
          (nameof this.EstimatedMonthlyIncome)
          this.EstimatedMonthlyIncome

      return this
    }

type CreateCategoryCommand = {
  BudgetId: Guid
  CategoryName: string
}

type CreateAllocatedCategoryCommand = {
  BudgetId: Guid
  CategoryName: string
  Allocation: float
}

type CreateExpenseCommand = {
  BudgetId: Guid
  CategoryName: string
  ExpenseAmount: float
  ExpenseDetails: string
}

type UserModel = {
  NameIdentifier: string
  EmailAddress: string
}

type CurrentUser =
  | NotAuthenticated
  | Authenticated of UserModel

type RpcResult<'a> =
  | Success of 'a
  | NoPermission
  | ErrorMessage of string

[<RequireQualifiedAccess>]
module RpcResult =
  let errorMessage rpcResult =
    match rpcResult with
    | Success _ -> None
    | NoPermission -> Some "You don't have permission to perform that command."
    | ErrorMessage errorMessage -> Some errorMessage

type AsyncRpcResult<'a> = Async<RpcResult<'a>>

type PublicApi = { GetUser: unit -> Async<CurrentUser> }

type SecuredApi = {
  GetBudgets: unit -> Async<Budget list>
  CreateBudget: CreateBudgetCommand -> AsyncRpcResult<Budget>
  CreateCategory: CreateCategoryCommand -> AsyncRpcResult<Budget>
  AllocateCategory: CreateAllocatedCategoryCommand -> AsyncRpcResult<Budget>
  CreateExpense: CreateExpenseCommand -> AsyncRpcResult<Budget>
}
