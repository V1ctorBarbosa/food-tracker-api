# Beverage units (g for food, ml for drinks)

Status: backend implemented (`feature/beverage-units-api`), frontend pending.
Affects: `food-tracker-api` (backend, docs, Firestore indexes) and `food-tracker-app` (frontend).
Related sections of `docs/food-track-implementation.md`: 13 to 19, rewritten by this feature.

---

## 1. Problem

Every food was treated as "per 100 g", drinks included. To log a can of soda the user had to think in grams, and the app showed "g" for something sold and drunk in ml.

The domain already supported a volume base unit (`BaseUnit` accepts `Gram` or `Milliliter`), but both external sources and the custom food flow hardcoded grams, and the app always sent `unit: 'Gram'`.

It also hid a data bug: for drinks, Open Food Facts reports nutrients per 100 ml even though the fields are named `*_100g` (its product API states `"nutrition_data_per": "100ml"`; checked on Coca-Cola 3 L, `7894900011708`). Those foods were stored as "per 100 g".

## 2. Decisions

| # | Decision |
|---|---|
| D1 | A food is measured in **grams** (food) or **milliliters** (drink). `BaseUnit.Unit` is the single source of truth; there is no separate flag. |
| D2 | The base quantity is always **100**. |
| D3 | The default quantity when logging is always **100** in the food's unit, never the package size. |
| D4 | **No household measures** (can, glass, bottle, spoon). |
| D5 | **Water entries stay separate.** A drink never counts toward the water goal. |
| D6 | **No new data source.** TACO and Open Food Facts are enough. |
| D7 | `FoodServing` and the `FoodServingId + ServingCount` path become unused and are removed in a **separate cleanup PR** (`improvement/remove-food-serving`). |
| D8 | Mass and volume are never converted into each other at calculation time. The only "100 g ≈ 100 ml" reading lives in the TACO mapper, as an import assumption. |
| D9 | **Yogurt stays in grams**, as Brazilian labels state it. |
| D10 | **Only custom foods are editable**, every field, including g ↔ ml. Shared TACO/Open Food Facts foods are never edited by users. |
| D11 | The one-off data fix is a **console project in the api solution** (`FoodTrack.Migrations`), reusing the same resolvers as the clients. |

## 3. Requirements

### R1. Open Food Facts classification

1. Read the unit from the free-text `quantity` (`"350ml"`, `"3 l"`, `"300 mL"`, `"1L"`, `"6 x 350 ml"`, `"500 g"`): the first unit symbol right after a number decides. `ml`, `cl`, `l` → `Milliliter`; `g`, `kg`, `mg` → `Gram`. A word that only starts with a unit letter ("1 lata") is not a unit.
2. No unit in `quantity`: `categories_tags` contains `en:beverages` → `Milliliter`.
3. Otherwise → `Gram`.

The nutrient values are used as they come. The search endpoint (search-a-licious) does not return `nutrition_data_per` or `product_quantity_unit`, but does return `quantity` and `categories_tags` (checked live on 2026-10-07).

### R2. TACO classification

`Milliliter` when the category is `Bebidas (alcoólicas e não alcoólicas)` or the id is one of the liquids filed elsewhere: juices 188, 209, 211, 213, 215, 217, 218, 219, 234, 252, 258, and liquid dairy 446, 454, 455, 457, 458, 460. Everything else is `Gram`, yogurts included (D9). Values are read as per 100 ml for those foods (density 0.95 to 1.05, error under 5%).

### R3. Custom food

- `POST /foods` takes an optional `baseUnitType` (`"Gram"` or `"Milliliter"`, any case). Left out it is `Gram`, so app builds already installed keep working.
- `PUT /foods/{id}` takes the same optional field. Left out, the food keeps its unit.
- Any other value (`Kilogram`, `Liter`, numbers, free text) → `400`.
- Changing the unit between g and ml moves the items already logged for that food to the new unit with the same number (350 g → 350 ml, 1 kg → 1 L), keeping their nutritional snapshot. Items move before the food is saved, so if the save fails, repeating the same edit finishes the job.

### R4. Logging a food

- The app sends the quantity in the food's own unit, defaulting to 100.
- Logging a unit of the other kind (grams of a drink, ml of a food) → `400` naming the food. It used to be a `500` (`InvalidOperationException` from the conversion).

### R5. Display (frontend)

- Unit labels: `Gram` → "g", `Milliliter` → "ml", `Kilogram` → "kg", `Liter` → "L".
- Search results show "por 100 g" or "por 100 ml"; the quantity panel and meal detail show the food's or item's unit.
- Macros stay in grams for drinks too.

### R6. Existing data

The search reads the cache first, so foods cached before this feature are never refreshed on their own. `FoodTrack.Migrations` fixes them once:

1. Lists every cached `Taco` and `OpenFoodFacts` food (custom foods are never touched).
2. Classifies each one: TACO by the category in its `foods.json`, fetched once; Open Food Facts by looking the product up by barcode (`/api/v2/product/{code}?fields=quantity,categories_tags`), one request every 700 ms to stay within the 100 reads/minute limit.
3. For each food whose unit changes: moves its logged items, then saves the food in the new unit.
4. A product the source no longer knows, or a failed request, is reported as skipped and left alone.
5. Runs as a dry run by default; `--Apply true` writes. Running it again changes nothing already right.

For Open Food Facts the relabelled items are exact: the stored nutrients were already per 100 ml, so the number the user typed was effectively ml. For TACO the same number is read with the R2 assumption.

## 4. Out of scope

- Household measures and serving presets (D4).
- Removing `FoodServing` (D7, separate PR).
- Counting drinks toward the water goal (D5).
- Logging in kg or L from the app.
- Editing shared foods (D10).

## 5. Design (as built in the backend)

**Domain**
- `MeasurementUnitCategory` (`Mass`, `Volume`) and `MeasurementUnit.GetCategory()`.
- `MeasurementUnitReclassification.Reclassify(unit, category)`: same number and scale in the other category.
- `Food.MeasuredIn(unit)`: the same food with its base unit moved, values unchanged.
- `NutritionCalculator.Calculate` rejects a unit of the other category with `ArgumentException` (R4), so every caller gets the same rule.

**Application**
- `CustomFoodBaseUnit.Resolve(requested, unitWhenMissing)` reads `baseUnitType` (R3).
- `CreateCustomFoodRequest` / `UpdateCustomFoodRequest` gain `BaseUnitType`.
- `FoodService.UpdateCustomAsync` calls `IMealRepository.ReclassifyItemUnitsAsync` when the category changes, then saves.
- `IFoodRepository.ListBySourceAsync` for the migration.

**Infrastructure**
- `OpenFoodFactsBaseUnitResolver` (R1) and `TacoBaseUnitResolver` (R2), pure and unit tested, used by both clients and by the migration.
- `FirestoreMealRepository.ReclassifyItemUnitsAsync`: a collection group query on `items` by `FoodId`, updating in batches of 500.
- `infra/firebase/firestore.indexes.json` enables the collection-group index on `items.FoodId` that query needs.

**Migration**
- `api/src/FoodTrack.Migrations`: `BaseUnitBackfill` (orchestration, unit tested), one `IFoodUnitClassifier` per source, a report formatter.

**Docs**
- Implementation doc sections 13 to 19 rewritten; Postman collection updated (create drink example, `baseUnitType` on update, meal unit note).

## 6. Rollout

The app ships **before** the backend. Today's app sends `unit: 'Gram'` for every item, so once the backend caches a drink as ml, that app gets a `400` logging it. The new app sends `food.baseUnitType`, which is `Gram` for everything the old backend returns, so it works against both.

1. Frontend PR (send `baseUnitType` as the item unit, labels, Comida/Bebida selector), then release the app.
2. Deploy the Firestore index: from `infra/firebase`, `firebase deploy --only firestore:indexes`. Wait until it shows as enabled in the console. Without it, changing a custom food between g and ml fails (other edits do not need it).
3. Merge the backend PR (the deploy workflow ships it).
4. Dry-run the migration, read the report, then run it with `--Apply true`.
5. Check a known drink (Coca-Cola) and an old meal containing it: the meal opens and saves, with the same numbers.
6. Separate PR: remove `FoodServing` (D7).

The Comida/Bebida selector in step 1 only has an effect once the backend is deployed; before that the backend ignores `baseUnitType` and saves grams.

## 7. Acceptance checks

- Searching "coca cola" shows "por 100 ml"; 350 ml of a 42 kcal/100 ml soda logs 147 kcal.
- Searching "arroz" still shows "por 100 g".
- "leite integral" (TACO 458) is ml; "iogurte natural" (TACO 448) is g.
- A custom "Bebida" is saved per 100 ml and logged in ml; switching it back to "Comida" moves its logged items to g.
- An app build without the new field can still create a custom food (as g).
- A meal item in `Gram` for a `Milliliter` food returns `400`, not `500`.
- After the migration, opening and saving an old meal that contains a drink works and its numbers are unchanged.
- Water screen and goal behave as before.
