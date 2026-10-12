# Meal estimate by AI (photo or description)

Status: draft, not implemented.
Affects: `food-tracker-api` (backend, infra, docs) and `food-tracker-app` (frontend).
Depends on: `beverage-units.md` for the "Bebida" option (see D2).
Related sections of `docs/food-track-implementation.md`: 10 to 12 (Meal, MealItem, nutritional snapshot).

---

## 1. Problem

Logging a meal today means searching and adding one food at a time. A plate with rice, beans, salad and chicken takes four searches and four quantity adjustments, and for home-cooked or restaurant food there is often no good catalog match anyway.

The user wants to describe a meal the way they would to a person: take a photo of the plate, write what it is, or both, and get every ingredient logged at once with its estimated quantity and nutrition.

## 2. Decisions

| # | Decision |
|---|---|
| D1 | The add-meal screen offers **four separate options**: **Comida**, **Bebida**, **Foto** and **Descrever**. They stay four distinct buttons in the UI even though the last two share one engine in the backend. |
| D2 | **Comida** and **Bebida** are the existing catalog flow (TACO, Open Food Facts, custom foods), one food at a time. Bebida is the same search restricted to `Milliliter` foods, so it needs `beverage-units.md` implemented first. |
| D3 | **Foto** takes a photo with an optional description. **Descrever** takes a description with no photo. Both go through a single estimate use case whose input requires at least one of the two. |
| D4 | The AI (Claude) identifies each ingredient **and estimates its nutrition by itself**. Estimated items are not matched against the catalog and never become `Food` records. |
| D5 | When the description contradicts the photo, **the description wins** (for example "arroz integral", "200 g de frango, pesei"). |
| D6 | The estimate returns, per item, a **quantity plus nutrition per 100 g/ml**, never final totals. The backend computes the totals, so editing the quantity recalculates exactly like a catalog item. |
| D7 | An estimate never saves anything by itself. It opens a review screen where each item is **toggled in or out** and the estimate can be **re-run with an edited description** on the same photo. Confirming adds the included items to the meal. Quantities are adjusted afterwards, on the meal detail, which keeps the review screen quick (layout follows `Food Track Prototype v4`). |
| D8 | All four options feed **the same meal**. A meal can mix catalog items and estimated items. |
| D9 | Photos are stored in **S3** (the API already runs on AWS Lambda in `us-east-2`). The app uploads directly to S3 through a presigned request; the image never passes through the Lambda payload. |
| D10 | The feature runs on the owner's **Anthropic API** account (billed separately from any Claude subscription). Access is limited to an **allowlist of user ids**; everyone else never sees the Foto/Descrever options and gets `403` from the endpoints. The rest of the app stays public. |
| D11 | The Anthropic API key lives in SSM Parameter Store under the existing API parameter path, written by hand, like the Firebase key. |

## 3. Requirements

### R1. Add-meal options

- Opening "adicionar refeição" (from the log tab or from a meal's "adicionar item") shows "Como quer registrar?" with four cards, as in the prototype: Alimentos, Bebidas, Foto do prato and Descrever, the last two tagged "IA", and the note "Foto e descrição geram uma estimativa. Você revisa os itens antes de salvar." (Comida and Bebida in this spec are those first two cards.)
- Foto and Descrever are only shown when the API reports the user can estimate meals (R8).
- Comida and Bebida open the existing search screen, Bebida restricted to drinks (D2).

### R2. Photo capture and upload

- Foto opens the camera ("Foto do prato", hint "Enquadre o prato inteiro, de cima, com boa luz."), with "Galeria" to pick an existing photo instead.
- After the photo is taken or picked, the screen shows it with an **optional description field** and the "Calcular com IA" button.
- Before upload the app resizes the image to at most **1024 px** on the longest side and encodes it as JPEG.
- The app asks the API for an upload slot. The API creates a `captureId`, derives the object key itself (`pending/{userId}/{captureId}.jpg`, R7) and returns a **presigned PUT** URL that:
  - is signed for `Content-Type: image/jpeg`, returned in `headers` for the app to send exactly,
  - expires in **5 minutes**.
- A presigned PUT cannot cap the body size, so the API checks it instead: a photo above **5 MB** is refused when it is estimated or saved on a meal.
- The client never sends an S3 key. Every later request refers to the `captureId`, and the API rebuilds the key from the signed-in user id, so one user can never point at another user's photo.

### R3. Estimate request

- Input: `captureId` (optional) and `description` (optional).
  - At least one is required, otherwise `400`.
  - Descrever: description required, **10 to 500 characters**.
  - Foto: description optional, **up to 500 characters**.
- When a `captureId` is given, the API checks the object exists under the user's prefix, otherwise `400`.
- The API calls Claude with the image (by a short-lived presigned GET URL) and/or the description, asking for structured output (`output_config.format` with a JSON schema), not free text.
- Each returned item has:
  - `name` in Brazilian Portuguese (for example "Arroz branco cozido"),
  - `quantity` and `unit` (`Gram` or `Milliliter`),
  - per 100 g/ml: `calories`, `protein`, `carbs`, `fat`.
- Validation of the model's answer, so a bad answer fails at the boundary instead of reaching a meal:
  - 1 to `Meal.MaximumItemCount` items,
  - quantity > 0 and at most 2000,
  - every nutrient >= 0, calories per 100 at most 900 (pure fat), and protein + carbs + fat per 100 at most 100.
- If the model reports that it found no food, the API answers `422` (`NoFoodRecognized`).
- If Claude fails or times out, the API answers `502` (`EstimateUnavailable`). The app shows a retry message.
- The response returns the `captureId` (minted by the API for Descrever) and the items with their computed totals (D6). Nothing is persisted.

### R4. Review screen ("Estimativa")

- While the estimate runs: "Calculando…" with "Identificando os alimentos da foto" or "Lendo sua descrição".
- Shows the source ("Pela foto" / "Pela descrição"), the totals of the included items (kcal, protein, carbs, fat) and the list of items, each with name, quantity and kcal.
- "Toque para incluir ou tirar": tapping an item toggles it. Every item starts included. Totals follow the included items.
- Footer note: "Valores estimados pela IA. Você pode ajustar cada item depois, na refeição."
- **Ajustar descrição e recalcular**: opens the description for editing and re-runs R3 with the same `captureId` (no new photo, no new upload). The new result replaces the items on screen and resets them all to included. For Descrever the same action edits the text that was sent.
- **Adicionar em {refeição}**: adds the included items to the meal (creating it, or appending to an existing one, like the catalog flow does today), then opens the meal detail. Disabled when no item is included.
- "Voltar" returns to the screen the estimate came from (photo or description).
- Items entering, leaving and being replaced animate (Reanimated `entering`/`exiting`, `layout={LinearTransition}` on the list), and toggling an item animates its included/excluded state.

### R4b. Editing an item's quantity on the meal detail

- Today the meal detail can only remove items (swipe to delete). It gains editing an item's quantity, for **both** catalog and estimated items.
- In edit mode, tapping an item opens a quantity panel in the item's unit (same step and minimum as the add-food screen), with the nutrition preview recalculating as the value changes.
- Saving re-sends the meal through the update endpoint. A catalog item recalculates from its `Food`; an estimated item recalculates from its stored per-100 values.

### R5. Estimated meal items

- A `MealItem` has one of two origins:
  - **catalog**: references a `Food` (today's behavior),
  - **estimated**: carries its own name, unit and nutrition per 100, plus the `captureId` it came from.
- Both keep the nutritional snapshot (quantity, calories, protein, carbs, fat), so totals, dashboard and charts treat them the same.
- The meal create/update request accepts both item kinds. An item sends exactly one of `foodId` or `estimatedFood` (name, `captureId`, base unit and per-100 values); the backend recomputes the snapshot from them and validates them with the same limits as R3. The response mirrors it: each item has either `foodId` or `estimatedFood`.
- The request carries the meal's `captures`. On update, omitting `captures` keeps the ones the meal already has, so an older app build can still edit a meal that has them.
- Re-saving a meal (the update endpoint replaces every item) keeps estimated items as they are: the client sends them back with their stored per-100 values.
- Meal detail shows the item's own name for estimated items, with a small "estimado por IA" mark.

### R6. Captures on the meal

- A meal stores the list of captures its estimated items came from: `captureId`, whether it has a photo, and the description used in the last analysis.
- Meal detail shows each capture's photo (presigned GET URL in the response, short TTL) and description.
- Captures are capped per meal (at most **5**), so one meal cannot accumulate unbounded paid analyses and photos.

### R7. Photo lifecycle

- An uploaded photo lands under the **`pending/`** prefix. An S3 lifecycle rule deletes everything under `pending/` after **1 day**, which covers estimates that were never confirmed. A prefix, not a tag, because a lifecycle filter matches a key prefix, and it keeps the upload a plain PUT.
- When a meal is saved with a capture, the API moves its photo to **`attached/{userId}/{captureId}.jpg`**, which no rule touches. A photo already attached cannot be estimated again.
- When a meal update drops a capture, its photo is deleted.
- Deleting a meal deletes its captures' photos.
- Deleting the account deletes everything under `users/{userId}/`. The existing delete-account flow must include this.
- A capture whose items were all removed from the meal keeps its photo until the meal is deleted (no orphan sweep in v1).

### R8. Access and cost control

- The API reads the allowlist of user ids from configuration (SSM). The upload-slot and estimate endpoints return `403` for anyone else.
- The profile response gains `canEstimateMeals` so the app knows whether to show Foto/Descrever.
- The estimate endpoints get their own per-user rate limit on top of the global one: **20 requests per hour**.
- A monthly spend limit is set by hand in the Anthropic console. That is the hard cap; the allowlist and rate limit only keep usage reasonable.

## 4. Out of scope

- Matching estimated items against the catalog, or turning them into custom foods.
- Editing an item's quantity, name or per-100 values on the review screen (it only toggles items; quantity is edited on the meal detail, R4b).
- Renaming an estimated item.
- More than one photo per capture, audio input.
- Opening the AI options to every user (that needs a billing model first).
- Sweeping photos of captures whose items were all removed (R7, last point).

## 5. Design

### Backend (`food-tracker-api`)

**Domain**
- `MealItem` gets an origin, modeled as a closed type with two cases rather than a nullable `FoodId`: `CatalogFoodOrigin(FoodId)` and `EstimatedFoodOrigin(EstimatedFood, CaptureId)`.
- `EstimatedFood` value object: name, `BaseUnit` (100 g or 100 ml) and nutrition per base, with the limits from R3 as its invariants. Its calculation reuses `NutritionCalculator` math for a quantity.
- `CaptureId` value object (GUID, like the other ids).
- `MealCapture` value object: `CaptureId`, `HasPhoto`, `MealDescription?`. `Meal` holds `IReadOnlyList<MealCapture>`, enforces the cap (R6) and that every estimated item points at a capture of the same meal.
- `MealDescription` value object: trimmed text, at most 500 characters.
- `MealCapture` is also the input of an estimate: its invariant is "a photo, or a description of at least 10 characters". There is no separate input type.
- A Descrever estimate has no upload slot, so the estimate use case mints its `CaptureId` and returns it with the items. Every Descrever re-run gets a new one; only the one confirmed ends up on the meal.

**Application**
- Port `IMealEstimator`: takes the image URL (optional) and the description (optional), returns the raw recognized items or "no food".
- Port `IMealPhotoStorage`: create the upload slot, read a photo's state (missing, too large, pending, attached), get a read URL for a pending or an attached photo, mark attached, delete one, delete a user's photos.
- `MealPhotoLifecycle`: keeps photos in step with a meal's captures (new photo captures must be uploaded, attached after save, dropped ones deleted, all deleted with the meal), so `MealService` only orchestrates.
- Port `IMealEstimateAccessPolicy`: whether a user id may use the feature (backed by the allowlist).
- `MealEstimateService`: `CreateUploadSlotAsync` and `EstimateAsync` (access check, input validation, storage check, estimator call, answer validation, totals).
- `MealService`: resolve both item kinds; on create/update check and attach new captures' photos and delete dropped ones; on delete remove photos.
- Account deletion use case: also calls `IMealPhotoStorage` to delete the user prefix.

**Infrastructure**
- `ClaudeMealEstimator` implements `IMealEstimator` with the official Anthropic SDK: model from configuration (default `claude-haiku-5-5`), image as a URL source, structured output with the R3 JSON schema (no tool: the answer is the JSON itself), the prompt with D5. HTTP timeout 25 s, one retry. A stop reason other than `end_turn` (refusal, `max_tokens`) counts as unavailable. Prompt, schema and answer parser live in `MealEstimatePrompt` and `RecognizedMealJsonParser`, apart from the SDK call.
- `S3MealPhotoStorage` implements `IMealPhotoStorage` (presigned PUT signed for the content type; HEAD for state and size; presigned GET; copy from `pending/` to `attached/`; delete; list a user's prefixes on account deletion). `MealPhotoKeys` builds every key.
- `ConfigurationMealEstimateAccessPolicy` reads the allowlist.
- `MealItemDocument` gains `Origin`, `Name`, `CaptureId` and the per-100 values; documents without `Origin` read as catalog. `MealDocument` gains `Captures`.

**Api**
- `POST /meal-estimates/upload-slots` → `{ captureId, uploadUrl, headers, expiresAt }`.
- `POST /meal-estimates` with `{ captureId?, description? }` → `{ items: [...] }`.
- Rate limit policy for both (R8). `NoFoodRecognized` → `422`, `EstimateUnavailable` → `502` in `GlobalExceptionHandler`.
- Meal endpoints: item request accepts the estimated kind; `MealDto` items gain `origin`, `name`, `captureId`; `MealDto` gains `captures` with photo URLs.
- Profile response gains `canEstimateMeals`.

**Infra**
- `terraform/bootstrap` (applied by hand, because the CI user may only manage the Lambda function): private S3 bucket `food-track-meal-photos-{accountId}` (block public access, owner-enforced, SSE-S3, lifecycle rule expiring `pending/` after 1 day), and the Lambda role gets `s3:PutObject`, `s3:GetObject`, `s3:DeleteObject` on its objects and `s3:ListBucket` on the bucket only. Output `meal_photos_bucket_name`.
- `terraform/app`: bucket name passed to the Lambda as `MealPhotos__BucketName`, from the `MEAL_PHOTOS_BUCKET_NAME` GitHub secret like the role ARN. Lambda `timeout` from 30 to **60** s, so two 25 s model attempts plus Firestore writes fit.
- SSM, by hand: `Anthropic/ApiKey` (SecureString) and `MealEstimates/AllowedUserIds` (comma-separated) under the existing API parameter path (D11).
- The zero-spend AWS budget will alert on the first S3 cent. Expected.

**Docs**
- `docs/food-track-implementation.md`: sections 10 to 12 cover the two item origins and captures.
- Postman: the two new endpoints, an estimated item in the meal create/update examples, and the new profile field.

### Frontend (`food-tracker-app`)

- `shared/lib/image-capture`: port for taking/picking and resizing a photo, one adapter over `expo-image-picker` + `expo-image-manipulator`. Features never import those packages directly.
- `entities/meal`: types for both item kinds and captures; the item request type becomes a union.
- `features/meal-estimate`: upload-slot + upload + estimate requests, and the mutations.
- `pages/add-meal`: the four-option hub (new route). The log tab and meal detail's "adicionar item" go here instead of straight to `add-food`.
- `pages/add-food`: receives a food/drink mode (D2).
- `pages/meal-photo`: camera/gallery, then photo preview with the optional description.
- `pages/meal-describe`: the description screen.
- `pages/meal-estimate`: the review screen (R4). Its derived logic lives in `model/`: totals of the included items, toggling, description length validation, source label, replacing the items after a re-run.
- `pages/meal-detail`: item name from the item itself when estimated, the "estimado por IA" mark, captures with photo and description, quantity editing (R4b). Building the request for re-saving a meal handles both kinds (today it maps every item to `foodId`). Recalculating an estimated item from its per-100 values lives in `entities/meal/model`, shared by the meal detail preview.
- User-facing text in pt-BR, code in English.

## 6. Tasks

Backend PR (`food-tracker-api`):

1. Domain: `CaptureId`, `MealDescription`, `EstimatedFood`, `MealCapture`, `MealItem` origins, `Meal` captures and invariants. Tests, including `[Theory]` for every limit in R3 and R6.
2. Persistence: document fields and mappers, old documents read as catalog. Tests.
3. `MealService` with both item kinds and `MealPhotoLifecycle` (check, attach, delete photos). Tests with substituted ports.
4. `MealEstimateService` + ports. Tests: access denied, empty input, missing object, no food, invalid model answer, happy path for photo, photo + description and description only.
5. `ClaudeMealEstimator`: tests for the schema, the prompt text and the parsing of the answer (pure parts extracted from the SDK call). The live call is an integration concern and is not unit tested.
6. `S3MealPhotoStorage` and the allowlist policy. Pure key-building logic unit tested; S3 calls are integration.
7. Endpoints, rate limit policy, error mapping, profile field, account deletion.
8. Infra (bucket, IAM, timeout), implementation doc and Postman.

Frontend PR (`food-tracker-app`), after the backend is deployed:

9. Image-capture port and adapter.
10. `entities/meal` types and the `features/meal-estimate` requests.
11. Add-meal hub and routing changes.
12. Photo and description screens, review screen and their `model/` functions with tests.
13. Meal detail for estimated items and captures, re-save for both kinds, with tests.
14. Quantity editing on the meal detail (R4b) for both kinds, with tests.

Rollout:

15. Before merging the backend PR: apply bootstrap by hand, add the `MEAL_PHOTOS_BUCKET_NAME` GitHub secret (the deploy fails without it), write the two SSM parameters, set the Anthropic monthly limit.
16. Deploy the backend, try both endpoints from Postman.
17. Release the app.

## 7. Acceptance checks

- With an allowed user, the add-meal screen shows four options; with any other user it shows only Comida and Bebida, and calling `/meal-estimates` directly returns `403`.
- A photo of rice, beans and chicken returns three items with quantities; adding them creates a meal with three estimated items and the photo shows on the meal detail.
- The same photo with "o frango é empanado e frito" returns more fat for the chicken than without it.
- "200 g de arroz integral" with no photo returns an item of 200 g of brown rice.
- Tapping an item on the review screen excludes it, the totals drop by its values, and it is not added to the meal.
- On the meal detail, changing an estimated item from 150 g to 300 g doubles its calories and macros; changing a catalog item recalculates from its food.
- "Ajustar descrição e recalcular" on a photo estimate replaces the items on screen without a new upload.
- "Calcular com IA" on Descrever stays disabled below 10 characters.
- A meal with one catalog item and two estimated items can be opened, have an item added and be saved again with all values unchanged.
- A photo of something that is not food returns the "não encontramos comida" message.
- An estimate never confirmed has its photo gone from S3 after a day.
- Deleting the meal deletes its photos; deleting the account deletes the user's whole prefix.
- The 21st estimate within an hour returns `429`.

## 8. Settled points

1. **Bebida before beverage units**: `beverage-units.md` is implemented first. This feature starts after it lands.
2. **Model**: Haiku (`claude-haiku-5-5`), kept in configuration so it can change without a deploy.
3. **Re-run cap**: none beyond the hourly rate limit of 20 estimates (R8).
