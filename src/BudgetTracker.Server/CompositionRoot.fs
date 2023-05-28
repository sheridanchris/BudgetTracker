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

let claimOrEmptyString (claimType: string) (ctx: HttpContext) =
  claim claimType ctx |> Option.defaultValue ""

let validateOrFail validationF =
  match validationF () with
  | Ok _ -> ()
  | Error _ -> failwith "Validation errors."

let createPublicApi (httpContext: HttpContext) = {
  GetUser =
    fun () ->
      async {
        return
          if httpContext.User.Identity.IsAuthenticated then
            printfn
              "User claims: %A"
              (httpContext.User.Claims
               |> Seq.map (fun claim -> $"{claim.Type}: {claim.Value}")
               |> String.concat ",")

            Authenticated {
              NameIdentifier = httpContext |> claimOrEmptyString ClaimTypes.NameIdentifier
              EmailAddress = httpContext |> claimOrEmptyString ClaimTypes.Email
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
          validateOrFail command.Validate

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

            match budget with
            | None -> return NoPermission
            | Some budget when budget.OwnerId <> userId -> return NoPermission
            | Some budget ->
              let newBudget =
                budget
                |> Budget.createOrUpdateAllocatedCategory command.CategoryName command.Allocation

              do! DataAccess.saveBudget documentSession newBudget
              return Success newBudget
          }
      CreateExpense =
        fun command ->
          async {
            let! budget = DataAccess.getBudgetById querySession command.BudgetId

            match budget with
            | None -> return NoPermission
            | Some budget when budget.OwnerId <> userId -> return NoPermission
            | Some budget ->
              let now = DateTime.UtcNow

              let newBudget =
                budget
                |> Budget.createExpense command.CategoryName command.ExpenseAmount command.ExpenseDetails now

              do! DataAccess.saveBudget documentSession newBudget
              return Success newBudget
          }
    }
