output "github_actions_access_key_id" {
  description = "Set this as the AWS_ACCESS_KEY_ID secret in the GitHub repo."
  value       = aws_iam_access_key.github_actions_deploy.id
}

output "github_actions_secret_access_key" {
  description = "Set this as the AWS_SECRET_ACCESS_KEY secret in the GitHub repo. Only shown once -- if you lose it, taint and recreate aws_iam_access_key.github_actions_deploy."
  value       = aws_iam_access_key.github_actions_deploy.secret
  sensitive   = true
}

output "lambda_execution_role_arn" {
  description = "Feed this into the app-level Terraform config's lambda_execution_role_arn variable."
  value       = aws_iam_role.lambda_execution.arn
}

output "meal_photos_bucket_name" {
  description = "Feed this into the app-level Terraform config's meal_photos_bucket_name variable."
  value       = aws_s3_bucket.meal_photos.bucket
}
