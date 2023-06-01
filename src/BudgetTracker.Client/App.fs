open System
open Browser
open Sutil
open Sutil.Core
open Sutil.CoreElements
open Sutil.Router
open Fable.Core.JsInterop
open Sutil.DaisyUI
open ApplicationContext
open Remote
open Domain

type Page =
  | Home
  | CreateBudget
  | ViewBudget of {| budgetId: Guid |}
  | Allocate of {| budgetId: Guid |}
  | CreateCategory of {| budgetId: Guid |}
  | NotFound
  | Unauthorized

let parsePage url =
  match url with
  | []
  | [ "budgets" ] -> Home
  | [ "budgets"; "create" ] -> CreateBudget
  | [ "budgets"; Route.Guid budgetId ] -> ViewBudget {| budgetId = budgetId |}
  | [ "budgets"; Route.Guid budgetId; "allocate" ] -> Allocate {| budgetId = budgetId |}
  | [ "budgets"; Route.Guid budgetId; "create_category" ] -> CreateCategory {| budgetId = budgetId |}
  | _ -> NotFound

let requiresAuthentication page =
  match page with
  | Home
  | NotFound
  | Unauthorized -> false
  | CreateBudget
  | ViewBudget _
  | Allocate _
  | CreateCategory _ -> true

type Model = { CurrentPage: Page }

let currentPage model = model.CurrentPage

type Msg = SetCurrentPage of Page

let init () =
  let url = Router.getCurrentUrl window.location
  { CurrentPage = parsePage url }, Cmd.none

let update msg model =
  match msg with
  | SetCurrentPage page -> { model with CurrentPage = page }, Cmd.none

let tryFindBudgetById id budgets =
  budgets |> List.tryFind (fun budget -> budget.Id = id)

let renderBudgetOrNotFound (budgetId: Guid) (render: Budget -> SutilElement) =
  Bind.el (
    globalContext .> budgets .> tryFindBudgetById budgetId,
    function
    | None -> NotFoundPage.view ()
    | Some budget -> render budget
  )

let view () =
  let model, dispatch = () |> Store.makeElmish init update ignore

  let _ =
    Navigable.listenLocation (Router.getCurrentUrl >> parsePage >> SetCurrentPage >> dispatch)

  Html.div [
    disposeOnUnmount [
      model
      globalContext
    ]
    Attr.className "h-screen w-full"

    Daisy.Navbar.navbar [
      Attr.className "bg-base-100 items-center"
      Html.div [
        Attr.className "flex-1"
        Html.a [
          Daisy.Button.buttonAttr
          Daisy.Button.ghost
          Attr.className "normal-case text-xl"
          Attr.text "BudgetTracker"
          Attr.href "/#/"
        ]
      ]
      Daisy.Navbar.navbarEnd [
        Bind.el (
          globalContext .> currentUser,
          fun currentUser ->
            match currentUser with
            | Authenticated user -> Html.p $"Welcome {user.EmailAddress}"
            | NotAuthenticated ->
              Html.a [
                Daisy.Button.buttonAttr
                Attr.text "Login"
                Attr.href "http://localhost:5000/api/authenticate" // TODO: hardcoded url ;(
              ]
        )
      ]
    ]

    Bind.el2 (model .> currentPage) (globalContext .> currentUser) (fun (currentPage, currentUser) ->
      match requiresAuthentication currentPage, currentUser with
      | true, NotAuthenticated -> text "Unauthorized"
      | false, _
      | true, Authenticated _ ->
        match currentPage with
        | Home -> HomePage.view ()
        | CreateBudget -> CreateBudgetPage.view ()
        | ViewBudget info -> renderBudgetOrNotFound info.budgetId ViewBudgetPage.view
        | Allocate info -> renderBudgetOrNotFound info.budgetId AllocatePage.view
        | CreateCategory info -> renderBudgetOrNotFound info.budgetId CreateCategoryPage.view
        | NotFound -> NotFoundPage.view ()
        | Unauthorized -> UnauthorizedPage.view ())
  ]

importSideEffects "./styles.css"
Program.mount ("sutil-app", view ()) |> ignore
