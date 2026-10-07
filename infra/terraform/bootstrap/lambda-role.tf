# The Lambda function's own execution role -- what the function itself is allowed to do once
# running (write logs), as opposed to the GitHub Actions role above (what CI is allowed to do to
# deploy it). Created here, in bootstrap, so the app-level config never needs IAM write access.
data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda_execution" {
  name               = "food-track-api-lambda-execution"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Lets the function read its own secrets (the Firebase service account key) from Parameter Store at
# startup. The parameters themselves are written by hand with `aws ssm put-parameter`, never by
# Terraform: the provider reads a managed parameter's decrypted value back into state, which would
# put the key in plain text in the one place it is meant to stay out of. SecureString parameters use
# the AWS-managed aws/ssm key, whose key policy already lets account principals decrypt through SSM.
data "aws_iam_policy_document" "lambda_read_api_parameters" {
  statement {
    sid       = "ReadTheApiParameterPath"
    actions   = ["ssm:GetParametersByPath"]
    resources = ["arn:aws:ssm:${var.aws_region}:${data.aws_caller_identity.current.account_id}:parameter${trimsuffix(var.api_parameter_store_path, "/")}"]
  }

  statement {
    sid       = "ReadTheApiParameters"
    actions   = ["ssm:GetParameter", "ssm:GetParameters"]
    resources = ["arn:aws:ssm:${var.aws_region}:${data.aws_caller_identity.current.account_id}:parameter${var.api_parameter_store_path}*"]
  }
}

resource "aws_iam_role_policy" "lambda_read_api_parameters" {
  name   = "read-food-track-api-parameters"
  role   = aws_iam_role.lambda_execution.name
  policy = data.aws_iam_policy_document.lambda_read_api_parameters.json
}
