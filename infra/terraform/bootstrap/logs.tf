# The function's log group. Lambda creates it on first invocation with no expiry, and error logs can
# carry a user id, so keeping them forever would outlive an account deletion indefinitely. Managed
# here, by the human-run bootstrap config, so the CI deploy user needs no CloudWatch permissions.
resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${var.lambda_function_name}"
  retention_in_days = var.lambda_log_retention_in_days
}

# Lambda already created the group before Terraform managed it; this adopts it instead of failing
# on "already exists". Harmless once the group is in state.
import {
  to = aws_cloudwatch_log_group.lambda
  id = "/aws/lambda/${var.lambda_function_name}"
}
