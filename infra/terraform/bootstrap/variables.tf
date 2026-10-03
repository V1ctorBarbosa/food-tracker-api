variable "aws_region" {
  description = "AWS region everything is deployed to. Must match the app config."
  type        = string
  default     = "us-east-2"
}

variable "lambda_function_name" {
  description = "Name of the Lambda function the app-level Terraform config manages. Kept in sync between the two configs by both defaulting to the same value."
  type        = string
  default     = "food-track-api"
}

variable "budget_alert_email" {
  description = "Email that receives the zero-spend budget alert. Set via a Terraform Cloud workspace variable, not committed here."
  type        = string
}
