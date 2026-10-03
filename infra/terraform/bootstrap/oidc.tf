# Originally OIDC-based (short-lived, per-run AWS credentials, no static keys). Switched to a
# plain IAM user + access key here because this AWS account is on the "Free Plan" -- it's a
# member of an AWS-managed Organization with a Service Control Policy that explicitly denies
# iam:CreateOpenIDConnectProvider (confirmed: every other IAM action used in this config, like
# CreateRole, worked fine -- only the OIDC provider itself is blocked). A static key is a real
# security downgrade (long-lived, no auto-rotation) but is the only path available without
# upgrading off the Free Plan, which isn't an option here. The permissions below stay just as
# narrow as they would have been under OIDC.
resource "aws_iam_user" "github_actions_deploy" {
  name = "food-track-api-github-actions-deploy"
}

resource "aws_iam_access_key" "github_actions_deploy" {
  user = aws_iam_user.github_actions_deploy.name
}

# Deliberately narrow: only the one Lambda function this repo deploys, and only PassRole for the
# one execution role it needs to attach -- not broad Lambda or IAM access. Managing the execution
# role itself stays a bootstrap-only, human-run operation.
data "aws_iam_policy_document" "github_actions_deploy" {
  statement {
    sid = "ManageTheOneLambdaFunction"
    actions = [
      "lambda:CreateFunction",
      "lambda:UpdateFunctionCode",
      "lambda:UpdateFunctionConfiguration",
      "lambda:GetFunction",
      "lambda:GetFunctionConfiguration",
      "lambda:GetPolicy",
      "lambda:ListVersionsByFunction",
      "lambda:GetFunctionCodeSigningConfig",
      "lambda:GetFunctionConcurrency",
      "lambda:GetRuntimeManagementConfig",
      "lambda:AddPermission",
      "lambda:RemovePermission",
      "lambda:TagResource",
      "lambda:UntagResource",
      "lambda:ListTags",
      "lambda:CreateFunctionUrlConfig",
      "lambda:UpdateFunctionUrlConfig",
      "lambda:GetFunctionUrlConfig",
      "lambda:DeleteFunctionUrlConfig",
    ]
    resources = [
      "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.lambda_function_name}",
    ]
  }

  statement {
    sid       = "PassLambdaExecutionRoleOnly"
    actions   = ["iam:PassRole"]
    resources = [aws_iam_role.lambda_execution.arn]

    condition {
      test     = "StringEquals"
      variable = "iam:PassedToService"
      values   = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_user_policy" "github_actions_deploy" {
  name   = "deploy-food-track-api"
  user   = aws_iam_user.github_actions_deploy.name
  policy = data.aws_iam_policy_document.github_actions_deploy.json
}

data "aws_caller_identity" "current" {}
