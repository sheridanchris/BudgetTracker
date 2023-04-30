#r "nuget: Fun.Build, 0.3.7"
#r "nuget: dotenv.net, 3.1.2"

open Fun.Build
open dotenv.net

let environmentVariables = DotEnv.Read()

let environmentVariableOrDefault defaultValue envVariableKey =
  match environmentVariables.TryGetValue envVariableKey with
  | true, var -> var
  | false, _ -> defaultValue

let redisPassword = "REDIS_PASSWORD" |> environmentVariableOrDefault "PASS"
let postgresPassword = "POSTGRES_PASSWORD" |> environmentVariableOrDefault "PASS"

pipeline "Build" {
  stage "Restore" {
    run "dotnet tool restore"
    run "dotnet restore"
    run "npm i"
  }

  stage "verify" {
    run "dotnet build"
    run "dotnet test"
  }

  runIfOnlySpecified false
}

pipeline "dev-client" {
  stage "run client" { run "npm start" }
  runIfOnlySpecified true
}

pipeline "dev-server" {
  envVars [
    "ConnectionStrings__Redis", $"localhost,password={redisPassword}"
    "ConnectionStrings__Postgresql", $"Host=localhost;Database=postgres;Username=postgres;Password={postgresPassword}"
    "GOOGLE_CLIENT_ID", environmentVariables["GOOGLE_CLIENT_ID"]
    "GOOGLE_SECRET", environmentVariables["GOOGLE_SECRET"]
    "LOGIN_REDIRECT_URL", "http://localhost:5173/#/"
    "ASPNETCORE_ENVIRONMENT", "Development"
  ]

  stage "run backend" {
    run "docker compose -f docker-compose.Development.yml down"
    run "docker compose -f docker-compose.Development.yml up -d --build"
    run "dotnet watch run --project ./src/BudgetTracker.Server/BudgetTracker.Server.fsproj"
  }

  runIfOnlySpecified true
}
