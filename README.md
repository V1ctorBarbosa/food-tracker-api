# Food Track API

Backend for **Food Track**, a mobile-first app for logging meals, water and weight, and tracking calories and macronutrients against daily targets. Meals can be logged from a food catalog or **estimated by AI from a photo or a short description**.

Built with **.NET 10 / C#**, following **Domain-Driven Design**, deployed to **AWS Lambda** with **Terraform** and **GitHub Actions**.

> Frontend (React Native + Expo): [food-tracker-app](https://github.com/V1ctorBarbosa/food-tracker-app)

## Highlights

- **AI meal estimate with Claude.** The user photographs a plate, describes it, or both. The API sends the image and text to Claude with a **JSON schema for structured output**, so the answer is always machine-readable. Claude returns each ingredient with an estimated quantity and nutrition per 100 g/ml; the **backend computes the totals**, so an estimated item behaves exactly like a catalog item when its quantity is edited. Nothing is saved until the user reviews the items.
- **Safe by design around the AI feature.** Photos go straight from the app to **S3 through presigned URLs** (never through the Lambda payload), object keys are derived server-side from the signed-in user, estimates are **rate limited per user**, access is restricted by an allowlist, and the Anthropic key lives in **SSM Parameter Store**.
- **Rich domain model.** Entities protect their own invariants (meals, meal items with a nutritional snapshot, foods with a base unit in grams or milliliters, water and weight entries), and value objects replace primitive types.
- **Nutrition catalog** backed by the Brazilian **TACO** table, **Open Food Facts** and user-created foods.
- **370+ unit tests** (xUnit, FluentAssertions, NSubstitute), run in CI before every deploy.

## Architecture

```
api/src
├── FoodTrack.Domain          entities, value objects, invariants (no dependencies)
├── FoodTrack.Application     use cases and the ports they depend on
├── FoodTrack.Infrastructure  Firestore, S3, Claude, TACO/Open Food Facts adapters
├── FoodTrack.Api             minimal API endpoints, auth, rate limiting, error handling
└── FoodTrack.Migrations      one-off data migrations
api/test/FoodTrack.Tests      mirrors src, one test file per class
```

- Dependencies point inward: **Api → Application → Domain**. Infrastructure implements the application's interfaces and is wired at the composition root, so the domain never touches Firestore, S3 or the Anthropic SDK.
- **Authentication** validates Firebase ID tokens (Google Sign-In). Every endpoint requires an authenticated user.
- **Firestore is reached only through this API** (Admin SDK). The Firestore security rules deny every direct client read and write.

## Infrastructure and delivery

- **AWS Lambda behind a Function URL**, defined in **Terraform** and split in two configs: a `bootstrap` config applied by hand (IAM, execution role, S3 bucket, budget alarm) and an `app` config applied by CI. State lives in Terraform Cloud.
- **GitHub Actions** runs the test suite and deploys on every push to `master` that touches the API or its infrastructure.
- A **zero-spend AWS budget alarm** flags any unexpected cost.

Details: [infra/README.md](infra/README.md)

## How it is built

- **Spec-driven development.** Each feature starts as a written spec with explicit decisions and requirements before any code: see [docs/specs](docs/specs), for example the [AI meal estimate spec](docs/specs/meal-ai-estimate.md).
- **AI-assisted engineering.** Developed with Claude Code inside a workflow of specs, small reviewed changes, tests with every change and Conventional Commits. The AI writes alongside me; the design decisions and the review are mine.
- The full design (domain model, nutrition calculations, dashboard, roadmap) is in the [implementation document](docs/food-track-implementation.md).
- Manual API testing: Postman collection in [docs/endpoints-test/postman](docs/endpoints-test/postman).

## Running locally

Requires the .NET 10 SDK.

```bash
dotnet test api/FoodTrack.slnx
dotnet run --project api/src/FoodTrack.Api
```

Running the API against real data needs a Firebase service account and, for the AI estimate, an Anthropic API key. Neither is included in the repository.
