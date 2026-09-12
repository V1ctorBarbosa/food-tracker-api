# Lets GitHub Actions assume an AWS role using short-lived, per-run tokens -- no long-lived AWS
# access keys stored anywhere. AWS no longer actually validates the thumbprint (they verify
# against their own trusted CA bundle instead), but the API still requires one to be supplied, so
# it's fetched live here rather than hardcoded to something that could silently go stale.
data "tls_certificate" "github_actions" {
  url = "https://token.actions.githubusercontent.com/.well-known/openid-configuration"
}

resource "aws_iam_openid_connect_provider" "github_actions" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.github_actions.certificates[0].sha1_fingerprint]
}

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github_actions.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Only a push to this exact branch of this exact repo can assume the role -- a PR from a
    # fork, or a push to any other branch, is rejected before AWS credentials are ever issued.
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.github_repository}:ref:refs/heads/${var.github_branch}"]
    }
  }
}

resource "aws_iam_role" "github_actions_deploy" {
  name               = "food-track-api-github-actions-deploy"
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
}

# Deliberately narrow: only the one Lambda function this repo deploys, and only PassRole for the
# one execution role it needs to attach -- not broad Lambda or IAM access. Managing the execution
# role itself, or the OIDC trust relationship, stays a bootstrap-only, human-run operation.
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

resource "aws_iam_role_policy" "github_actions_deploy" {
  name   = "deploy-food-track-api"
  role   = aws_iam_role.github_actions_deploy.id
  policy = data.aws_iam_policy_document.github_actions_deploy.json
}

data "aws_caller_identity" "current" {}
