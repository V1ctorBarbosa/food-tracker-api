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
   (`organization = "food-track"` is already set in both `main.tf` files — rename the org to
   match if you called it something else).
2. Create two workspaces in it: `food-track-api-bootstrap` and `food-track-api`, choosing the
   **CLI-Driven Workflow** for both (not "Version control workflow" — Terraform runs on your
   machine / in GitHub Actions, not on Terraform Cloud's own runners; it's only used here to
   store state and secrets).
3. Generate a user API token: *User settings → Tokens*. You'll use this in step 2 below and as a
   GitHub secret.

### 2. Run `terraform login`

```bash
terraform login
```

Paste the API token from the step above when prompted. This writes it to a local credentials
file Terraform CLI reads automatically — you don't pass it on the command line.

### 3. Apply the bootstrap config

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply
```

This needs real AWS credentials on your machine (e.g. `aws configure`) — it's the one and only
time this project needs your own AWS credentials directly; everything after this uses the IAM
user it creates, whose permissions are scoped to just this one Lambda function.

Note the outputs: `github_actions_access_key_id`, `lambda_execution_role_arn`, and
`github_actions_secret_access_key` (hidden by default since it's sensitive -- run
`terraform output -raw github_actions_secret_access_key` to reveal it. It's only ever shown once;
if you lose it, `terraform taint aws_iam_access_key.github_actions_deploy` and re-apply to
generate a new one).

### 4. Configure the app workspace

In the Terraform Cloud UI, on the `food-track-api` workspace, add these workspace variables:

| Key | Value | Category | Sensitive |
|---|---|---|---|
| `lambda_execution_role_arn` | the bootstrap output | Terraform variable | no |
| `firebase_credentials_json` | contents of your `firebase-service-account.json` | Terraform variable | **yes** |

(`firebase_project_id` and `lambda_function_name` already default correctly in `variables.tf` —
only override them here if you want different values.)

### 5. Add GitHub repo secrets

On `V1ctorBarbosa/food-tracker-api` → *Settings → Secrets and variables → Actions*, add:

| Secret | Value |
|---|---|
| `AWS_ACCESS_KEY_ID` | the `github_actions_access_key_id` output from step 3 |
| `AWS_SECRET_ACCESS_KEY` | the `github_actions_secret_access_key` output from step 3 |
| `TF_API_TOKEN` | the same Terraform Cloud token from step 1 |

That's it. Push to `master` and the `Deploy API` workflow builds, tests, and deploys.

## Verifying a deploy manually

```bash
cd infra/terraform/app
terraform output -raw function_url
```

`curl` that URL + `health` (no auth needed) to confirm the function is live, or `health/firebase`
with a real Firebase ID token in the `Authorization: Bearer` header.
