variable "aws_region" {
  description = "AWS region everything is deployed to."
  type        = string
  default     = "us-east-1"
}

variable "lambda_function_name" {
  description = "Name of the Lambda function the app-level Terraform config manages. Kept in sync between the two configs by both defaulting to the same value."
  type        = string
  default     = "food-track-api"
}
