module CompositionRoot

open System
open Microsoft.AspNetCore.Http
open System.Security.Claims
open Marten
open Remote
open DomainLogic

let claim (claimType: string) (ctx: HttpContext) =
  ctx.User.Claims
  |> Seq.tryFind (fun claim -> claim.Type = claimType)
  |> Option.map (fun claim -> claim.Value)

let createPublicApi (httpContext: HttpContext) = {
  GetUser =
    fun () ->
      async {
        return
          if httpContext.User.Identity.IsAuthenticated then
            Authenticated {
              NameIdentifier = httpContext |> claim ClaimTypes.NameIdentifier |> Option.defaultValue ""
              EmailAddress = httpContext |> claim ClaimTypes.Email |> Option.defaultValue ""
            }
          else
            NotAuthenticated
      }
}

let createSecuredApi (httpContext: HttpContext) =
  let querySession = httpContext.GetService<IQuerySession>()
  let documentSession = httpContext.GetService<IDocumentSession>()

  match claim ClaimTypes.NameIdentifier httpContext with
  | None -> {
      GetBudgets = fun () -> Async.singleton []
      CreateBudget = fun _ -> Async.singleton NoPermission
      AllocateCategory = fun _ -> Async.singleton NoPermission
      CreateExpense = fun _ -> Async.singleton NoPermission
    }
  | Some userId -> {
      GetBudgets = fun () -> DataAccess.getBudgetForUser querySession userId |> Async.map Seq.toList
      CreateBudget =
        fun command ->
          async {
            let budgetId = Guid.NewGuid()

            let budget =
              Budget.create budgetId userId command.Name command.Description command.EstimatedMonthlyIncome

            do! DataAccess.saveBudget documentSession budget
            return Success budget
          }
      AllocateCategory =
        fun command ->
          async {
            let! budget = DataAccess.getBudgetById querySession command.BudgetId

            return
              match budget with
              | None -> NoPermission
              | Some budget when budget.OwnerId <> userId -> NoPermission
              | Some budget ->
                budget
                |> Budget.createOrUpdateAllocatedCategory command.CategoryName command.Allocation
                |> Success
          }
      CreateExpense =
        fun command ->
          async {
            let! budget = DataAccess.getBudgetById querySession command.BudgetId

            return
              match budget with
              | None -> NoPermission
              | Some budget when budget.OwnerId <> userId -> NoPermission
              | Some budget ->
                let now = DateTime.UtcNow

                budget
                |> Budget.createExpense command.CategoryName command.ExpenseAmount command.ExpenseDetails now
                |> Success
          }
    }
