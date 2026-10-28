# AWS-only backend (Cognito + DynamoDB + CloudFront)

Status: proposed, nothing built.
Affects: `food-tracker-api` (backend, infra, migrations, docs) and `food-tracker-app` (frontend auth adapter, web hosting).
Related sections of `docs/food-track-implementation.md`: authentication, persistence and hosting, rewritten once this ships.

---

## 1. Problem

Auth (Firebase Auth), the database (Firestore) and the web build (Firebase Hosting) live on Google; everything else (Lambda, S3, Parameter Store) lives on AWS. Two clouds means two consoles, two sets of credentials (the Firebase service account key sits in Parameter Store only so the Lambda can reach Google) and two bills.

The goal is to run on AWS only, with three hard constraints:

1. **No data is lost.** Every profile, meal, item, food, water and weight entry, the monthly estimate counter and every meal photo stays reachable by the same person after the move.
2. **Sign-in stays exactly as it is today.** The same "Entrar com Google" button, the same native Google sheet on Android/iOS, the same popup on web. No browser redirect to a hosted login page.
3. **It stays free.** Every AWS service used must fit an always-free allowance at the app's usage (section 7).

The app has **one user, its owner**. That removes the need for anything that only exists to protect other users during the switch (dual token schemes, maintenance mode, in-app notices).

## 2. Decisions

| # | Decision |
|---|---|
| D1 | **Existing users keep their Firebase uid as their `UserId`.** Nothing stored is rewritten with a new id. This also keeps every S3 photo key valid, since `MealPhotoKeys` builds keys from the `UserId`. |
| D2 | `UserId` becomes an id **owned by the app**, no longer "the identity provider's uid". New users get a generated id (`Guid`, `N` format: no `/`, so it stays a valid S3 key segment). |
| D3 | A person is identified by their **Google subject** (`sub` of the Google ID token). It belongs to the Google account, not to Firebase or to an OAuth client, so it stays the same across the move. An **identity link** `GOOGLE#<googleSub> → UserId` is the only mapping. |
| D4 | Sign-in uses **Cognito's custom authentication flow**. The app keeps the native Google SDK, gets the same Google ID token as today, and exchanges it for Cognito tokens. The Cognito Hosted UI is **not** used. |
| D5 | The exchange goes through **one anonymous API endpoint**, `POST /auth/google`, so the app never needs to know Cognito usernames or create users. Refreshing tokens goes straight to Cognito. |
| D6 | Cognito users are created **lazily** on their first `POST /auth/google` (username `google_<googleSub>`), never imported up front. Self sign-up is off: only the API creates users. |
| D7 | The database moves to a **single DynamoDB table in provisioned mode**, inside the always-free 25 RCU / 25 WCU. On-demand mode is not covered by the free tier. Point-in-time recovery and deletion protection on. |
| D8 | **Meal items are embedded** in the meal item (they were a subcollection). A meal becomes one write, so the Firestore transactions in `UpdateAsync`/`DeleteAsync` disappear. |
| D9 | **Each switch is a single cut**, not a period where old and new run side by side: the owner is the only user and updates their own app at the same time the API changes. Rollback is redeploying the previous version, which works because Firebase stays untouched until the very end (D11). |
| D10 | The data copy is a **command in `FoodTrack.Migrations`**: dry run by default, `--Apply true` writes, idempotent, and it ends with a **verification report**. A mismatch fails the run. |
| D11 | **Firebase is deleted last**, only after everything runs on AWS and has worked correctly through the observation period (section 6, step 6). Before deletion, a final export of the Firestore data is kept in S3. |
| D12 | Cognito challenge Lambdas are written in **C#**, in a new `FoodTrack.AuthChallenges` project in the api solution, so they share the language, typing and test setup of the rest of the backend. |
| D13 | The web build moves from Firebase Hosting to **S3 + CloudFront**: private bucket, CloudFront Origin Access Control, served on the distribution's default `*.cloudfront.net` domain. No custom domain, so no Route 53 hosted zone (the one piece that would cost money every month). |
| D14 | Google sign-in needs OAuth clients, and today they live in the Google Cloud project that Firebase created; deleting Firebase deletes them. They move to a **standalone Google Cloud project** that holds only the OAuth clients (web, Android, iOS). It is free and is the one piece outside AWS that cannot be avoided while signing in with Google. |

## 3. Requirements

### R1. Sign-in (frontend)

1. The sign-in screen and its button do not change.
2. Native: `GoogleSignin.signIn()` exactly as today, configured with the new OAuth client ids (D14). The resulting `idToken` is sent to `POST /auth/google` instead of `signInWithCredential`.
3. Web: Google Identity Services returns a Google ID token from a popup, which goes to the same endpoint. The popup replaces Firebase's `signInWithPopup`; the user sees the same Google account chooser.
4. The session survives app restarts: tokens are stored in `expo-secure-store` on native and in `localStorage` on web.
5. The access token is refreshed before it expires with Cognito `InitiateAuth` (`REFRESH_TOKEN_AUTH`). A refresh that fails with an auth error signs the user out, as an expired Firebase session does today.
6. Sign-out clears the stored tokens and calls Cognito `RevokeToken` on the refresh token.
7. Everything above lives in one adapter, `cognito-auth-gateway.ts`, implementing the existing `AuthGateway`. No page, feature or entity changes. `http-client.ts` keeps asking `authGateway.getIdToken()`.

### R2. `POST /auth/google` (backend)

Request `{ "googleIdToken": "..." }`, anonymous.

1. Verifies the Google ID token: signature against Google's keys, issuer, expiry, `aud` in the configured list of Google client ids, `email_verified` true. Any failure → `401` with the existing `ApiErrorResponse` shape and code `TokenInvalid`.
2. Resolves the `UserId` (R3).
3. Creates the Cognito user `google_<googleSub>` if it does not exist (`AdminCreateUser`, message suppressed, `email` attribute set).
4. Runs `AdminInitiateAuth` with `CUSTOM_AUTH` and answers the challenge with the same Google ID token (`AdminRespondToAuthChallenge`).
5. Returns `{ accessToken, idToken, refreshToken, expiresIn }`.
6. Rate limited per IP, since it is the one anonymous endpoint that does work.

### R3. Resolving the `UserId`

1. An identity link `GOOGLE#<googleSub>` exists → its `UserId`. The migration writes the owner's link up front (R8.1), so this is the path for every existing user.
2. Otherwise a new `UserId` (D2), and a link is written for it.

Writing a link uses a conditional put (`attribute_not_exists`), so two concurrent first sign-ins end up with the same `UserId`.

### R4. Cognito custom challenge

- `DefineAuthChallenge`: one `CUSTOM_CHALLENGE` round; success when it is answered correctly, failure otherwise. Three failed rounds end the session.
- `CreateAuthChallenge`: no secret to generate; marks the challenge as "Google ID token".
- `VerifyAuthChallenge`: verifies the answer as a Google ID token with the same rules as R2.1, and checks its `sub` matches the Cognito username (`google_<sub>`). Verified here and not only in the API because `CUSTOM_AUTH` can be started by anyone holding the public app client id.

### R5. Authenticating API requests

1. A Cognito access token is validated as a JWT: issuer is the user pool, signature against the pool's JWKS, `token_use` is `access`, `client_id` is the app client (Cognito access tokens carry `client_id`, not `aud`, so this is checked explicitly).
2. The `username` claim (`google_<googleSub>`) gives the Google subject; the identity link gives the `UserId`, which becomes the `NameIdentifier` claim exactly as the Firebase uid is today. Link lookups are cached like the session status.
3. Session status: the Cognito user missing or disabled → `TokenRevoked`, through `IUserSessionStatusProvider` and the existing `CachedUserSessionStatusProvider`.
4. Cognito has no "tokens valid after" timestamp. Access tokens live **1 hour** (the same as a Firebase ID token), so `AdminUserGlobalSignOut` on an enabled user takes effect within that hour. Deleting or disabling the user takes effect immediately (after the status cache).
5. JWT failures map to the existing codes: expired → `TokenExpired`, anything else → `TokenInvalid`. The `401` body does not change, so the app's handling does not either.
6. Firebase tokens are no longer accepted once this ships (D9).

### R6. Account deletion

`AccountDeletionService` keeps its order. `IIdentityAccountRemover` is implemented by `CognitoIdentityAccountRemover`, which removes the Cognito user and the identity link. Each removal treats "not found" as done, so a retried deletion still finishes.

### R7. Data model (DynamoDB)

One table, `PK`/`SK` strings, plus three global secondary indexes:

| Entity | `PK` | `SK` | GSI1 (`GSI1PK` / `GSI1SK`) | GSI2 (`GSI2PK` / `GSI2SK`) | GSI3 (`GSI3PK` / `GSI3SK`) |
|---|---|---|---|---|---|
| User profile | `USER#<userId>` | `PROFILE` | | | |
| Meal (items embedded) | `MEAL#<mealId>` | `MEAL` | `USER#<userId>#MEAL` / `<loggedAt>` | | |
| Water entry | `WATER#<id>` | `WATER` | `USER#<userId>#WATER` / `<loggedAt>` | | |
| Weight entry | `WEIGHT#<id>` | `WEIGHT` | `USER#<userId>#WEIGHT` / `<loggedAt>` | | |
| Food | `FOOD#<foodId>` | `FOOD` | `USER#<ownerId>#FOOD` / `<foodId>` (custom only) | `FOOD` / `<nameLower>` | `SOURCE#<source>` / `<externalId>` |
| Estimate usage | `ESTIMATE_USAGE` | `<yyyy-MM>` | | | |
| Identity link | `GOOGLE#<googleSub>` | `LINK` | | | |

`<loggedAt>` is ISO 8601 UTC with fixed precision, so string order is time order.

Provisioned capacity, all within the free 25 RCU / 25 WCU shared by the table and its indexes: table 7/7, each GSI 6/6. Auto scaling off, so the bill can never grow on its own. At one user this is far more than needed; a burst above it is throttled and retried by the SDK, never billed.

How each repository method maps:

| Today (Firestore) | DynamoDB |
|---|---|
| Get meal/water/weight by id, then check the owner | `GetItem` by `PK`, same owner check |
| List meals/water/weight of a user in a day range | `Query` GSI1, `GSI1SK BETWEEN from AND to`, ascending |
| Latest weight entry | `Query` GSI1, descending, `Limit 1` |
| Meal add / update (batch, transaction over the subcollection) | one `PutItem` |
| Meal delete (transaction) | one `DeleteItem` |
| `ReclassifyItemUnitsAsync` (collection group query on `items.FoodId`) | `Scan` with `contains(FoodIds, :foodId)` on a `FoodIds` string set stored on each meal, then one `UpdateItem` per affected meal. Acceptable: it only runs when a custom food switches between g and ml. |
| Food search (range on `NameLower` from the first word, then `FoodNameMatcher`) | `Query` GSI2 with `begins_with(GSI2SK, firstWord)`, then the same `FoodNameMatcher` and the same filtering as today |
| Food by source + external id | `Query` GSI3, `GSI3SK = externalId`, `Limit 1` |
| Foods by source (migrations) | `Query` GSI3 by `GSI3PK` |
| Custom foods of a user | `Query` GSI1 |
| Delete everything of a user | `Query` GSI1 per kind, then `BatchWriteItem` in chunks of 25, retrying unprocessed items |
| Monthly estimate counter (transaction) | `UpdateItem ADD Count :one` with `attribute_not_exists(Count) OR Count < :limit`; a failed condition means "limit reached" |
| User profile get / save / delete | `GetItem` / `PutItem` / `DeleteItem` |

Meals saved before totals were stored are filled in by the migration (the items are embedded, so the totals are computed once there), so `ToSummaryAsync` no longer needs its "load items for old meals" path.

### R8. Data migration command

`dotnet run --project src/FoodTrack.Migrations -- firestore-to-dynamodb` (Firebase credentials + AWS dev profile), dry run unless `--Apply true`:

1. **Identity links.** Lists every Firebase Auth user; for each one with a `google.com` provider writes `GOOGLE#<googleSub> → <firebase uid>`. A user without a Google provider is reported, not linked.
2. **Data.** Reads every document of `users`, `meals` (with its `items`), `foods`, `waterEntries`, `weightEntries` and `mealEstimateUsage`, maps it **through the domain** (Firestore mapper → domain object → DynamoDB mapper), and writes it with `PutItem` on deterministic keys. Running it twice gives the same table. Going through the domain means a document the domain rejects is reported with its id and reason, never silently dropped or written half-valid.
3. **Pacing.** Writes are paced to stay under the table's provisioned write capacity (R7), so the copy never needs capacity raised (and never costs anything). A throttled write is retried, not skipped.
4. **Verification report**, per user and per kind:
   - document count in Firestore vs item count in DynamoDB;
   - meal item count per meal;
   - per day: total kcal, protein, carbs, fat, water ml and the weight entries, Firestore vs DynamoDB;
   - custom food count per owner, cached food count per source;
   - every rejected document and every unlinked Firebase user.
5. The command exits non-zero when anything in 4 differs or anything was rejected. The cutover (section 6) does not proceed on a non-zero run.

S3 photos are not touched: their keys already use the `UserId` that D1 keeps.

### R9. Final Firestore export

`dotnet run --project src/FoodTrack.Migrations -- firestore-export` writes every Firestore collection (subcollections included) and the Firebase Auth user list as one JSON file per collection, and uploads them to a private `backups/firestore/<date>/` prefix in S3. It runs once, right before Firebase is deleted (section 6, step 7).

### R10. Web hosting

1. `expo export --platform web` output is synced to a private S3 bucket; CloudFront serves it through Origin Access Control. The bucket is never public.
2. Unknown paths return `index.html` (`403`/`404` → `/index.html` with `200`), so deep links and a page reload inside the app keep working as on Firebase Hosting.
3. `index.html` is served `no-cache`; hashed assets are cached for a year.
4. The public account deletion page keeps working at the same path under the new domain.
5. The new `https://<distribution>.cloudfront.net` origin is added to `web_app_origins` (API CORS) and to the web OAuth client's authorized JavaScript origins (D14).
6. The account deletion URL registered in the Play Console is updated to the new domain.

## 4. Out of scope

- Moving the meal photos or changing their keys (not needed, D1).
- Sign-in methods other than Google.
- A custom domain for the web build (D13).
- Changing any endpoint other than adding `POST /auth/google`.

## 5. Design

**Domain**
- `UserId`: summary reworded to "the app's own user id"; validation unchanged.
- `GoogleSubject` value object (non-empty, the Google `sub`).
- `IdentityLink` (`GoogleSubject` → `UserId`).

**Application**
- `Users/Authentication/GoogleSignInService`: verify token → resolve `UserId` (R3) → ensure identity account → issue session. Ports:
  - `IGoogleIdTokenVerifier` → `VerifiedGoogleIdentity` (subject, email);
  - `IIdentityLinkRepository` (`FindAsync`, `TryAddAsync`);
  - `IIdentitySessionIssuer` (create user if missing, run the custom flow) → `IdentitySession`.
- `UserIdFactory` for new ids (D2).

**Infrastructure**
- `Persistence/DynamoDb/`: `DynamoDbOptions` (table name), `DynamoDbKeys` (every prefix in R7 as named constants, pure and unit tested), one `*Item` + `*ItemMapper` per aggregate, one `DynamoDb*Repository` per repository interface, `DynamoDbBatchWriter` (chunks of 25 + unprocessed retry).
- `DynamoDbMealEstimateQuota` replacing `FirestoreMealEstimateQuota`; `MealEstimateUsageMonth` stays as is.
- `DynamoDbIdentityLinkRepository`.
- `Cognito/`: `CognitoOptions` (pool id, app client id), `CognitoIdentitySessionIssuer`, `CognitoIdentityAccountRemover`.
- `Google/GoogleIdTokenVerifier` (on `Google.Apis.Auth`), `GoogleOptions` (accepted client ids).
- `FirestoreWarmUp` is dropped from the API wiring once DynamoDB is live; DynamoDB needs no warm-up.

**Api**
- `Authentication/CognitoTokenValidation` on `JwtBearer`, plus an `OnTokenValidated` step that swaps the `username` claim for the `UserId` and applies the session status (R5).
- `CognitoUserSessionStatusProvider` (`AdminGetUser`), wrapped by the existing `CachedUserSessionStatusProvider`.
- `JwtAuthErrorCodeMapper` replacing `FirebaseAuthErrorCodeMapper`.
- `POST /auth/google` endpoint, with its request/response DTOs and per-IP rate limit.

**AuthChallenges** (`api/src/FoodTrack.AuthChallenges`)
- Three handlers (R4), with the decision logic in plain classes so it is unit tested without Lambda.

**Migrations**
- `FirestoreToDynamoDb/`: `IdentityLinkImport`, `DataCopy` (one copier per kind), `WritePacing`, `MigrationVerifier`, `MigrationReportFormatter`.
- `FirestoreExport/`: `FirestoreExporter`, writing to S3 (R9).
- Orchestration, pacing, comparison and formatting are unit tested with fakes.

**Frontend** (`food-tracker-app`)
- `shared/api/cognito-auth-gateway.ts` implementing `AuthGateway`.
- `shared/api/token-storage.native.ts` / `.web.ts`, `shared/api/cognito-token-refresh.ts`.
- `google-sign-in.native.ts` returns the Google ID token instead of calling Firebase; `google-sign-in.web.ts` uses Google Identity Services.
- `shared/config/google-config.ts` with the new OAuth client ids (D14), replacing `firebase-config.ts`.
- `auth-gateway.ts` re-exports the Cognito adapter.

**Infra** (`infra/terraform`)
- `aws_dynamodb_table` per R7: `PROVISIONED` with the capacities in R7, `STANDARD` table class (the free storage only covers it), point-in-time recovery, `deletion_protection_enabled`.
- `aws_cognito_user_pool` on the Lite feature plan (admin-create only, no self sign-up, the three Lambda triggers), app client without secret, `ALLOW_CUSTOM_AUTH` + `ALLOW_REFRESH_TOKEN_AUTH`, access/ID token 1 h, refresh token 30 days.
- The three challenge Lambdas and their invoke permissions for Cognito.
- Web: private S3 bucket, CloudFront distribution with Origin Access Control, the error responses and cache policies in R10, default certificate.
- Backups: the `backups/` prefix (R9) in a private bucket, no public access.
- Bootstrap role: `dynamodb:*Item`, `Query`, `Scan`, `BatchWriteItem` on the table and its indexes; `cognito-idp:AdminCreateUser`, `AdminGetUser`, `AdminInitiateAuth`, `AdminRespondToAuthChallenge`, `AdminDeleteUser`, `AdminUserGlobalSignOut` on the pool.
- Lambda environment: table name, pool id, app client id, Google client ids.
- An AWS Budgets alert at **USD 0.01** on the account, so any charge at all is noticed the same day.

**CI**
- The web deploy job syncs the export to the bucket and invalidates `/index.html` in CloudFront, replacing the Firebase Hosting deploy.

**Docs**
- Postman: `POST /auth/google` in the API collection; `{{idToken}}` documented as a Cognito access token.
- Implementation doc: authentication, persistence and hosting sections rewritten.

**Tests**
- Unit (mirrored under `test/FoodTrack.Tests`): key builders, every item mapper, `GoogleSignInService`, `UserIdFactory`, `GoogleSubject`, the JWT error mapper, the claim swap, the challenge decision classes, the migration orchestration, pacing, verifier, exporter and report formatter, and a regression case with real exported data shapes (an old meal without stored totals, a drink in ml, a custom food).
- Integration (called out, not unit): DynamoDB repositories against DynamoDB Local in Testcontainers; the Cognito and Google adapters are exercised in the dev environment, not mocked into unit tests.

## 6. Rollout

Each step is its own PR against `master`, in this order. Nothing ships before the step above it is live. Firebase is never written to or changed by any step until step 7, so every rollback is "redeploy the previous version".

1. **DynamoDB in place, not used.** Table in Terraform, DynamoDB repositories, migration command. The API still reads and writes Firestore.
2. **Rehearsal.** Run the migration with `--Apply true` against the new table, read the report. Repeat until it exits zero. Nothing changes for the app.
3. **Database cutover.** The owner stops using the app for the few minutes it takes.
   1. Run the migration again with `--Apply true` (idempotent, picks up everything written since the rehearsal). It must exit zero.
   2. Deploy with the DynamoDB repositories wired in.
   3. Smoke test: dashboard, an old meal with its photo, the weight history; log and delete a meal.
   - Rollback: redeploy the previous version. Firestore was not written to after step 3.1, so it is still complete; re-run the migration later.
4. **Auth cutover.** Standalone Google Cloud project with the OAuth clients (D14), user pool, challenge Lambdas, `POST /auth/google`, Cognito token validation, identity link import. Deploy the API and install the new app build together.
   - Check: sign in on the phone with the same Google account through the same native sheet, and see the full history.
   - Rollback: redeploy the previous API and reinstall the previous build.
5. **Web hosting move.** S3 + CloudFront per R10, CORS and OAuth origins updated, Play Console deletion URL updated, CI deploy switched. Firebase Hosting keeps serving the old build until step 7.
6. **Observation period: two weeks of normal daily use** with everything on AWS. It is over only when, for the whole period:
   - no `5xx` and no unexpected `401` in the API's CloudWatch logs;
   - sign-in, token refresh after a day away, and the web build all worked;
   - the AWS bill and the budget alert show USD 0.00.
   Any failure fixes forward or rolls back, and the two weeks restart.
7. **Retire Firebase.**
   1. Run the final export (R9) and check the files in S3 open and have the expected counts.
   2. PR removing Firebase from the code: Firestore repositories and mappers, `Firebase/`, `FirebaseAuthenticationHandler`, `FirebaseUserSessionStatusProvider`, `FirebaseAuthErrorCodeMapper`, `FirebaseAdmin` and `Google.Cloud.Firestore` packages, `infra/firebase`, Firebase Terraform variables and the Parameter Store key, and the Firebase packages in the app. The migration commands that read Firestore go too; the export in S3 is the archive.
   3. The owner deletes the Firebase project in the Firebase console (not automated).

## 7. Cost

Every piece fits an always-free allowance at one user. Figures as published by AWS in October 2026; check the pricing pages again before step 1, since the budget alert is the real guard.

| Service | Free allowance | Expected use | Monthly cost |
|---|---|---|---|
| DynamoDB | 25 GB storage, 25 RCU, 25 WCU (provisioned, Standard class) | a few MB, the capacities in R7 | USD 0.00 |
| DynamoDB point-in-time recovery | not in the free tier, billed per GB | a few MB | well under USD 0.01 |
| Cognito user pool (Lite) | 10,000 monthly active users | 1 | USD 0.00 |
| Lambda (API + 3 challenge functions) | 1 M requests, 400,000 GB-seconds | a few thousand requests | USD 0.00 |
| CloudFront | 1 TB transfer, 10 M requests (pay-as-you-go free tier) | a few hundred MB, a few thousand requests | USD 0.00 |
| S3 (web build, photos, backups) | not free beyond the first year, but S3 → CloudFront transfer is free | tens of MB | well under USD 0.01 |
| Parameter Store (standard) | free | a few parameters | USD 0.00 |
| Google Cloud project (OAuth clients only) | free | | USD 0.00 |

What would make it cost money, and is avoided on purpose: DynamoDB on-demand mode or auto scaling (D7, R7), a Route 53 hosted zone for a custom domain (D13), Cognito's Essentials or Plus plans, CloudFront flat-rate plans (they also do not apply to accounts on the AWS free plan).

## 8. Acceptance checks

- The migration report exits zero with equal counts and equal daily totals for every user.
- After step 3, a meal logged before the move opens with the same items, numbers and photo.
- On Android, tapping "Entrar com Google" shows the same native account sheet as before; on web, the same Google popup. No browser page from AWS appears.
- The owner signs in with the new app and sees all their history; their `UserId` is still their Firebase uid.
- A brand-new Google account gets a new `UserId` and an empty dashboard.
- Reloading the web build on a deep link (for example the account deletion page) loads that page, not an error.
- Deleting an account removes its DynamoDB items, photos, identity link and Cognito user; repeating the deletion succeeds.
- Disabling a Cognito user makes their next request return `401 TokenRevoked`.
- The monthly estimate limit still stops at the configured number, including under two concurrent requests.
- The estimate allowlist (`MealEstimate:AllowedUserIds`) still works unchanged, since it holds `UserId`s and D1 keeps them.
- After step 7, the app still signs in (the OAuth clients live in the standalone project, D14) and the final export is readable in S3.
- The AWS bill for the observation period is USD 0.00.
