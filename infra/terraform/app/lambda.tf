resource "aws_lambda_function" "api" {
  function_name = var.lambda_function_name
  role          = var.lambda_execution_role_arn
  # Handler is just the assembly name for Amazon.Lambda.AspNetCoreServer.Hosting -- the same
  # Program.cs that runs under Kestrel locally is the Lambda entry point, no separate handler
  # class needed. See AddAWSLambdaHosting in api/src/FoodTrack.Api/Program.cs.
  handler = "FoodTrack.Api"
  runtime = "dotnet10"

  timeout     = 30
  memory_size = 512

  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      Firebase__ProjectId       = var.firebase_project_id
      Firebase__CredentialsJson = var.firebase_credentials_json
      ASPNETCORE_ENVIRONMENT    = "Production"
    }
  }
}

# authorization_type = "NONE" makes AWS auto-attach the public-invoke resource policy on
# creation -- no separate aws_lambda_permission resource needed (confirmed against the provider's
# own docs). The app already authenticates every request itself via the Firebase bearer token,
# the same trust model the local dev server already has.
resource "aws_lambda_function_url" "api" {
  function_name      = aws_lambda_function.api.function_name
  authorization_type = "NONE"
}
