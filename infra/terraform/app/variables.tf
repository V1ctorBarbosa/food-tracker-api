variable "aws_region" {
  description = "AWS region everything is deployed to. Must match the bootstrap config, and must be the account's selected sign-up region: the AWS-managed SCP denies Lambda in every other region, us-east-1 included."
  type        = string
  default     = "us-east-2"
}

variable "lambda_function_name" {
  description = "Must match lambda_function_name in the bootstrap config -- that's what the GitHub Actions role's permissions are scoped to."
  type        = string
  default     = "food-track-api"
}

variable "lambda_execution_role_arn" {
  description = "Output of the bootstrap config's lambda_execution_role_arn. Set once as a Terraform Cloud workspace variable; this config never creates or modifies IAM itself."
  type        = string
}

variable "lambda_zip_path" {
  description = "Path to the zip produced by `dotnet lambda package`, built fresh in CI immediately before each apply."
  type        = string
}

variable "firebase_project_id" {
  description = "Firebase/GCP project id."
  type        = string
  default     = "food-track-d7a9e"
}

variable "firebase_credentials_json" {
  description = "Full JSON content of the Firebase service account key."
  type        = string
  sensitive   = true
}
