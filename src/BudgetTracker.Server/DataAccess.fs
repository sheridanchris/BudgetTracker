module DataAccess

open Marten
open Domain

let getBudgetForUser querySession userId =
  querySession
  |> Session.query<Budget>
  |> Queryable.filter <@ fun budget -> budget.OwnerId = userId @>
  |> Queryable.toListAsync

let getBudgetById querySession budgetId =
  querySession
  |> Session.query<Budget>
  |> Queryable.filter <@ fun budget -> budget.Id = budgetId @>
  |> Queryable.tryHeadAsync

let saveBudget documentSession budget =
  Session.storeSingle budget documentSession
  Session.saveChangesAsync documentSession
