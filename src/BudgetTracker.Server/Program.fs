open System
open System.Text.Json.Serialization
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Marten
open Marten.Services
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Weasel.Core
open Giraffe
open Saturn
open Domain

let configuration = ConfigurationBuilder().AddEnvironmentVariables().Build()

let configureMarten (storeOptions: StoreOptions) =
  storeOptions.Connection(configuration.GetConnectionString("Postgresql"))
  storeOptions.RegisterDocumentType<Budget>()

  let serializer =
    SystemTextJsonSerializer(EnumStorage = EnumStorage.AsString, Casing = Casing.CamelCase)

  // https://www.jannikbuschke.de/blog/fsharp-marten/
  serializer.Customize(fun options ->
    options.Converters.Add(
      JsonFSharpConverter(
        JsonUnionEncoding.AdjacentTag
        ||| JsonUnionEncoding.NamedFields
        ||| JsonUnionEncoding.UnwrapRecordCases
        ||| JsonUnionEncoding.UnwrapOption
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions
        ||| JsonUnionEncoding.AllowUnorderedTag,
        allowNullFields = false
      )
    ))

  storeOptions.Serializer(serializer)
  storeOptions.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate

let configureServices (serviceCollection: IServiceCollection) =
  serviceCollection.AddMarten(configureMarten).ApplyAllDatabaseChangesOnStartup()
  |> ignore

  serviceCollection
    .AddAuthorization()
    .AddAuthentication(fun options ->
      options.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
      options.DefaultChallengeScheme <- GoogleDefaults.AuthenticationScheme
      options.DefaultAuthenticateScheme <- GoogleDefaults.AuthenticationScheme)
    .AddGoogle(fun options ->
      options.ClientId <- configuration["GOOGLE_CLIENT_ID"]
      options.ClientSecret <- configuration["GOOGLE_SECRET"]
      options.CallbackPath <- "/google-callback")
    .AddCookie(fun options ->
      options.Cookie.Name <- "auth"
      options.LoginPath <- "/api/authenticate"
      options.Cookie.HttpOnly <- true
      options.Cookie.SameSite <- SameSiteMode.Strict
      options.Cookie.SecurePolicy <- CookieSecurePolicy.Always)
  |> ignore

  serviceCollection

let routeBuilder (typeName: string) (methodName: string) = $"/api/{typeName}/{methodName}"

let errorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
  Propagate {|
    path = routeInfo.path
    msg = ex.Message
    stackTrace = ex.StackTrace
  |}

let googleAuthenticate: HttpHandler =
  fun next (ctx: HttpContext) ->
    task {
      let redirectUrl = configuration["LOGIN_REDIRECT_URL"]
      let properties = AuthenticationProperties(RedirectUri = redirectUrl)
      do! ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties)
      return! next ctx
    }

let publicApi: HttpHandler =
  Remoting.createApi ()
  |> Remoting.fromContext CompositionRoot.createPublicApi
  |> Remoting.buildHttpHandler

let securedApi: HttpHandler =
  Remoting.createApi ()
  |> Remoting.fromContext CompositionRoot.createSecuredApi
  |> Remoting.buildHttpHandler

let requiresAuth: HttpHandler =
  setStatusCode 401 >=> text "You are not authenticated!"

let router: HttpHandler =
  choose [
    publicApi
    requiresAuth >=> securedApi
    route "/api/authenticate" >=> GET >=> googleAuthenticate
  ]

let app =
  application {
    url "http://0.0.0.0:5000"
    service_config configureServices
    app_config (fun builder -> builder.UseAuthentication().UseAuthorization())
    use_router router
  }

run app
