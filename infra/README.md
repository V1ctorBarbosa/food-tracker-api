# Infrastructure

The API deploys to AWS Lambda, behind a Function URL (not API Gateway — API Gateway's free tier
is only free for 12 months; Lambda's is "Always Free" and never expires). All of it is defined in
Terraform under `terraform/`, split into two configs:

- **`terraform/bootstrap/`** — the IAM user GitHub Actions authenticates as, scoped to only this
  Lambda function, and the Lambda function's own execution role. Applied once, by hand, from your
  own machine. GitHub Actions never touches this config or has permission to change it.
  (Originally designed around OIDC federation -- short-lived, per-run credentials, no static keys
  stored anywhere. Switched to a plain IAM user + access key because this AWS account is on the
  "Free Plan", which blocks `iam:CreateOpenIDConnectProvider` via an AWS-managed Service Control
  Policy. If you ever upgrade off the Free Plan, switching back to OIDC is a reasonable follow-up.)
- **`terraform/app/`** — the Lambda function and its Function URL. Applied automatically by
  GitHub Actions on every push to `master` that touches `api/` or `infra/terraform/app/`.

## One-time setup

You only do this once. After it's done, every future deploy is just `git push`.

### 1. Terraform Cloud (free, no card)

1. Create an account at [app.terraform.io](https://app.terraform.io) and an organization
   (`organization = "v1ctorbarbosa"` is already set in both `main.tf` files — rename the org to
   match if you called it something else).
2. Create two workspaces in it: `food-track-api-bootstrap` and `food-track-api`, choosing the
   **CLI-Driven Workflow** for both (not "Version control workflow" — Terraform runs on your
   machine / in GitHub Actions, not on Terraform Cloud's own runners; it's only used here to
   store state and secrets).
3. Generate a user API token: *User settings → Tokens*. You'll use this in step 2 below and as a
   GitHub secret.

### 2. Upgrade the AWS account to Paid Plan

This account was created through AWS's managed sign-up flow, so it sits inside an AWS-owned
Organization whose Service Control Policies cannot be edited from here. One of them
(`UsEast1Partitional`) denies every non-global service in `us-east-1`, and `RegionFloor` only
allows the region picked at sign-up (`us-east-2` for this account). That is why Lambda calls in
`us-east-1` came back `AccessDeniedException`, and why everything here deploys to `us-east-2`.
Lambda itself is a supported Free Tier service. Activating "Advanced features" would lift the
policies but is irreversible and removes the account's spend limit, so it is deliberately not used.

1. Optional, but kept here: the account was upgraded to Paid Plan (AWS Console → *AWS Settings*).
   Always Free usage (Lambda's 1M requests/month and 400K GB-seconds) stays free under its
   published caps either way.
2. On the `food-track-api-bootstrap` Terraform Cloud workspace, add a `budget_alert_email`
   Terraform variable (your email, not sensitive), or type it when `terraform apply` prompts.
   Bootstrap also provisions a zero-spend AWS Budget (`budget.tf`) that emails that address the
   instant real spend appears. It is an alert only; the spend limit set in AWS Settings is the
   hard cap.

### 3. Run `terraform login`

```bash
terraform login
```

Paste the API token from the step above when prompted. This writes it to a local credentials
file Terraform CLI reads automatically — you don't pass it on the command line.

### 4. Apply the bootstrap config

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

This needs real AWS credentials on your machine (e.g. `aws configure`) — it's the one and only
time this project needs your own AWS credentials directly; everything after this uses the IAM
user it creates, whose permissions are scoped to just this one Lambda function.

Bootstrap also creates the private S3 bucket for AI meal estimate photos (`meal-photos.tf`) and
gives the function's role access to it.

Note the outputs: `github_actions_access_key_id`, `lambda_execution_role_arn`, `meal_photos_bucket_name`, and
`github_actions_secret_access_key` (hidden by default since it's sensitive -- run
`terraform output -raw github_actions_secret_access_key` to reveal it. It's only ever shown once;
if you lose it, `terraform taint aws_iam_access_key.github_actions_deploy` and re-apply to
generate a new one).

### 5. Store the Firebase service account key in Parameter Store

The function reads the key at startup from an encrypted SecureString parameter; it is never passed
through Terraform or GitHub, so it stays out of the Lambda configuration and out of Terraform state.

```bash
aws ssm put-parameter --region us-east-2 --name /food-track/api/Firebase/CredentialsJson --type SecureString --value file://firebase-service-account.json
```

To rotate the key later, run the same command with `--overwrite` and a new key file; the function
picks it up on its next cold start.

AI meal estimates need two more parameters: the Anthropic API key (billed to the Anthropic account
that owns it, separately from any Claude subscription) and the comma-separated Firebase user ids
allowed to use the feature. Anyone not on the list never sees the photo and description options.

```bash
aws ssm put-parameter --region us-east-2 --name /food-track/api/Anthropic/ApiKey --type SecureString --value "<anthropic api key>"
aws ssm put-parameter --region us-east-2 --name /food-track/api/MealEstimates/AllowedUserIds --type String --value "<firebase uid>"
```

Set a monthly spend limit on the Anthropic console as well: it is the hard cap on what estimates
can cost, while the allowlist, the per-user rate limit (20 estimates an hour) and the monthly cap
(300 estimates across all users, `MealEstimates:MonthlyEstimateLimit`) only keep usage
reasonable.

### 6. Set the Terraform Cloud execution mode to Local

On both workspaces, *Settings → General → Execution mode → Local*. Terraform then runs on your
machine and in GitHub Actions, and Terraform Cloud only stores state. In this mode workspace
variables set in the Terraform Cloud UI are ignored, so the app config's inputs come from the
GitHub secrets below instead.

### 7. Add GitHub repo secrets

On `V1ctorBarbosa/food-tracker-api` → *Settings → Secrets and variables → Actions*, add:

| Secret | Value |
|---|---|
| `AWS_ACCESS_KEY_ID` | the `github_actions_access_key_id` output from step 4 |
| `AWS_SECRET_ACCESS_KEY` | the `github_actions_secret_access_key` output from step 4 |
| `TF_API_TOKEN` | the same Terraform Cloud token from step 1 |
| `LAMBDA_EXECUTION_ROLE_ARN` | the `lambda_execution_role_arn` output from step 4 |
| `MEAL_PHOTOS_BUCKET_NAME` | the `meal_photos_bucket_name` output from step 4 |

(`firebase_project_id`, `lambda_function_name` and `api_parameter_store_path` already default
correctly in `variables.tf`.)

That's it. Push to `master` and the `Deploy API` workflow builds, tests, and deploys.

## Verifying a deploy manually

```bash
cd infra/terraform/app
terraform output -raw function_url
```

`curl` that URL + `health` (no auth needed) to confirm the function is live, or `health/firebase`
with a real Firebase ID token in the `Authorization: Bearer` header.
