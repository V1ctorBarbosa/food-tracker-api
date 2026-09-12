output "github_actions_role_arn" {
  description = "Set this as the AWS_ROLE_ARN secret in the GitHub repo."
  value       = aws_iam_role.github_actions_deploy.arn
}

output "lambda_execution_role_arn" {
  description = "Feed this into the app-level Terraform config's lambda_execution_role_arn variable."
  value       = aws_iam_role.lambda_execution.arn
}
