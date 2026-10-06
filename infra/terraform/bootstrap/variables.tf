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

variable "lambda_log_retention_in_days" {
  description = "How long the function's CloudWatch logs are kept. Must be one of the values CloudWatch accepts (1, 3, 5, 7, 14, 30, 60, 90, ...)."
  type        = number
  default     = 30
}

variable "budget_alert_email" {
  description = "Email that receives the zero-spend budget alert. Set via a Terraform Cloud workspace variable, not committed here."
  type        = string
}
