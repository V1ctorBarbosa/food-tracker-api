variable "aws_region" {
  description = "AWS region everything is deployed to."
  type        = string
  default     = "us-east-1"
}

variable "github_repository" {
  description = "GitHub repo allowed to assume the deploy role, as \"owner/repo\"."
  type        = string
  default     = "V1ctorBarbosa/food-tracker-api"
}

variable "github_branch" {
  description = "Branch allowed to assume the deploy role -- only pushes to this branch can deploy."
  type        = string
  default     = "master"
}

variable "lambda_function_name" {
  description = "Name of the Lambda function the app-level Terraform config manages. Kept in sync between the two configs by both defaulting to the same value."
  type        = string
  default     = "food-track-api"
}
