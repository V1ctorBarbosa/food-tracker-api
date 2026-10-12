# Food Track — Implementation Document

## 1. Overview

Personal, mobile-first application for tracking diet, calorie/macronutrient/water intake, and weight progress.

Main goals:

- Log meals daily.
- Log foods and quantities consumed.
- Calculate calories, protein, carbohydrates, and fat.
- Log water intake throughout the day.
- Set a daily calorie target based on a desired deficit.
- Track weight over time, without requiring a daily entry.
- Display charts for weight, calorie intake, macronutrient intake (protein, carbs, fat), and water intake.
- Let the user view any of these metrics individually, or combine two or more of them on the same chart (e.g. calories alone, protein alone, or calories + protein + water together).
- Support views by day, week, month, and longer periods.
- Start as a personal application, but keep the domain ready for future multi-user usage.

The application must remain simple in the MVP. The intent is not to build a professional nutrition system from the start.

---

# 2. Architecture

## Stack

### Frontend
- React Native
- TypeScript
- Expo

### Backend
- .NET / C#
- REST API

### Infrastructure
- Firebase Authentication
- Cloud Firestore
- Firebase Admin SDK on the backend

### Nutritional data
- Reliable external food API/database
- Local cache of foods in Firestore

## Flow

```text
┌─────────────────────────┐
│   React Native + Expo   │
│       TypeScript        │
└────────────┬────────────┘
             │ HTTPS / JSON
             ▼
┌─────────────────────────┐
│        .NET API         │
│                         │
│ Auth / Authorization    │
│ Meals                   │
│ Foods                   │
│ Weight                  │
│ Water                   │
│ Dashboard               │
│ Nutrition calculations  │
└────────────┬────────────┘
             │
             │ Firebase Admin SDK
             ▼
┌─────────────────────────┐
│       Firestore         │
│                         │
│ Users                   │
│ Meals                   │
│ MealItems               │
│ WeightEntries           │
│ WaterEntries            │
│ Foods                   │
└─────────────────────────┘
             ▲
             │
             │ HTTPS
             ▼
┌─────────────────────────┐
│ External Food API / DB  │
└─────────────────────────┘
```

## Main architectural rule

The frontend will NOT access Firestore directly.

The frontend only talks to the .NET API.

The .NET API is responsible for:

- authentication/authorization;
- user identification;
- Firestore access;
- external nutrition API access;
- business rules;
- nutritional calculations;
- aggregated dashboard data.

This avoids exposing secrets/credentials and keeps the application domain on the backend.

---

# 3. Why Expo?

Expo is a platform/toolset that simplifies React Native application development.

The idea is to keep working with React + TypeScript, while reducing the need to deal directly with the full native complexity of Android/iOS.

It also makes future features easier, such as:

- camera;
- notifications;
- local storage;
- native features;
- Android/iOS builds;
- offline support.

Choice:

```text
React Native
+
Expo
+
TypeScript
```

---

# 4. Authentication

The first version may be used only by the owner, but the model must be ready for multiple users.

Chosen technology:

- Firebase Authentication.

The API must validate the authenticated user's identity before accessing data belonging to them.

## Critical rule

Never trust a `userId` sent by the frontend.

Example:

```http
POST /meals
Authorization: Bearer <token>
```

The API validates the token and obtains:

```text
authenticatedUserId = 019...
```

Then creates:

```text
Meal.UserId = authenticatedUserId
```

Even if the frontend sends a different `userId`, the API must not allow that value to change the entity's owner.

---

# 5. Identifiers

Users and entities must have independent IDs.

Conceptually:

```text
User
id = user identifier

Meal
id = meal identifier
userId = owner
```

Therefore:

- `userId` identifies the user.
- `mealId` identifies the meal.
- `mealId` must not be used to infer the user.

GUID/UUID is a suitable option. The exact format can be defined during implementation.

---

# 6. Conceptual model

Main entities:

```text
User
Meal
MealItem
WeightEntry
WaterEntry
Food
```

Relationships:

```text
User
 ├── Meals
 │    └── MealItems
 │           └── Food
 │
 ├── WeightEntries
 │
 └── WaterEntries
```

The dashboard is not a persisted entity.

---

# 7. User

Represents the user's profile and settings.

Conceptual fields:

```text
User
--------------------------
id
name
age
height
gender
activityLevel
targetWeight
desiredCalorieDeficit
dailyCalorieTarget
dailyWaterTarget
dailyProteinTarget
dailyCarbsTarget
dailyFatTarget
```

Example:

```json
{
  "id": "019...",
  "name": "Victor",
  "age": 27,
  "height": 175,
  "gender": "...",
  "activityLevel": "moderate",
  "targetWeight": 80,
  "desiredCalorieDeficit": 500,
  "dailyCalorieTarget": 2000,
  "dailyWaterTarget": 4000,
  "dailyProteinTarget": 140,
  "dailyCarbsTarget": 190,
  "dailyFatTarget": 55
}
```

`gender` is one of `Male`, `Female`, `TransMale`, `TransFemale`, `Nonbinary`, `Other`.

`targetWeight`, `dailyWaterTarget`, `dailyProteinTarget`, `dailyCarbsTarget` and `dailyFatTarget` are all optional goals — a user may set none, some, or all of them. Only `dailyCalorieTarget` and `desiredCalorieDeficit` are required. These goals exist so the dashboard (Section 33) can render a reference line per metric (e.g. a 4 L/day water goal) alongside the user's actual history for that metric.

## Current weight

The current weight does not need to be the primary source on `User`.

History lives in `WeightEntries`.

Current weight can be obtained from the most recent entry.

This avoids unnecessary duplication.

---

# 8. WeightEntry

Represents a historical weight record.

Conceptual fields:

```text
WeightEntry
--------------------------
id
userId
date
weight
```

Example:

```text
09/03 → 87.6 kg
09/04 → 87.4 kg
09/05 → 87.8 kg
09/06 → no entry
09/07 → 87.2 kg
```

The user does not need to log weight every day.

## Important

Do not create entities such as:

```text
WeightProgression
DailyProgress
MonthlyProgress
```

Progression is derived from historical records.

---

# 9. WaterEntry

Represents water intake logged during the day.

Unlike `WeightEntry` (a single point-in-time measurement), water intake is naturally logged as several small entries throughout the day — similar in spirit to meals — and summed into a daily total.

Conceptual fields:

```text
WaterEntry
--------------------------
id
userId
date
amount
unit
```

Example:

```text
09/03
08:00 → 250 ml
11:30 → 300 ml
14:00 → 500 ml
19:00 → 250 ml

Total: 1300 ml
```

Water is logged in the volume units described in section 15 (`ml`, `L`).

## Important

Do not persist an entity such as:

```text
DailyWaterTotal
```

As with weight, calories, and macronutrients, the daily water total is derived by summing the day's `WaterEntry` records, not stored on its own.

---

# 10. Meal

Represents a meal that took place.

Conceptual fields:

```text
Meal
--------------------------
id
userId
loggedAt
mealType
totalCalories
totalProtein
totalCarbs
totalFat
```

`loggedAt` is a real timestamp (date + time), not just a date — set by the server to "now" when the meal is created, and preserved (not reset) when items are later added or edited. The calendar date used for grouping into a day (e.g. the dashboard's daily history) is derived from it rather than stored separately, the same way `WaterEntry.loggedAt` already works (Section 9).

Possible types:

```text
breakfast
lunch
dinner
snack
```

Totals can be calculated from `MealItems`.

It may be useful to persist the totals to make dashboard queries easier, as long as the API keeps them consistent when items change.

A meal also keeps its **captures**: the AI estimates (a photo, a description, or both) its estimated items came from (`docs/specs/meal-ai-estimate.md`). A capture records its id, whether it has a photo and the description used. A meal has at most 5 captures, every estimated item must point at one of them, and a capture stays on the meal even after all of its items are removed, so its photo is cleaned up when the meal is deleted.

---

# 11. MealItem

Represents a food consumed within a meal.

Conceptual fields:

```text
MealItem
--------------------------
id
mealId
foodId
quantity
unit
calories
protein
carbs
fat
```

Example:

```json
{
  "foodId": "chicken-breast",
  "quantity": 150,
  "unit": "g",
  "calories": 248,
  "protein": 46,
  "carbs": 0,
  "fat": 5
}
```

A `MealItem` has one of two **origins**:

- **Catalog**: it references a `Food` (`foodId`), as in the example above.
- **Estimated**: an AI estimate recognized it. There is no `Food`; the item carries the food itself, a name plus nutrition per 100 g or 100 ml, and the id of the capture it came from. It never becomes a catalog `Food`.

Both keep the same snapshot (quantity, calories, protein, carbs, fat), so totals, the dashboard and the charts treat them alike, and a meal can mix them. Keeping an estimated item's values per 100, not as totals, is what lets its quantity be edited later and recalculated exactly like a catalog item.

---

# 12. Nutritional snapshot

The `MealItem` must store the nutritional values used at the moment of consumption, in addition to the reference to `Food`.

Example:

Today:

```text
Chicken breast
165 kcal / 100g
```

User logs:

```text
200g
```

The system calculates:

```text
330 kcal
```

The `MealItem` saves the result.

If the `Food` is later corrected to:

```text
150 kcal / 100g
```

the old meal still represents the values used at the time.

Therefore:

```text
Food
```

represents the current nutritional information.

```text
MealItem
```

stores a snapshot of the values used at the time of logging.

---

# 13. Food

Represents a food available in the local database.

The local database acts as a cache of the external API.

Conceptual fields:

```text
Food
--------------------------
id
name
source
externalId
baseUnit
caloriesPerBaseUnit
proteinPerBaseUnit
carbsPerBaseUnit
fatPerBaseUnit
```

Example:

```text
Chicken breast

Base unit:
100g

Calories:
165

Protein:
31g

Carbs:
0g

Fat:
3.6g
```

---

# 14. Nutritional data source

Do not manually register every food.

Use a reliable external nutritional source.

One option discussed is USDA FoodData Central.

The integration must be done by the backend.

## Flow

```text
User searches:
"Chicken breast"

        ↓

.NET API

        ↓

Firestore

        ├── found
        │      ↓
        │   returns Food
        │
        └── not found
               ↓
        External Food API
               ↓
        saves Food to Firestore
               ↓
        returns Food
```

After the first lookup, the food becomes available locally.

**Implemented as two external sources queried together**, not one (`FoodTrack.Infrastructure/Foods/CompositeFoodSource`):
TACO (NEPA/UNICAMP, via the community-maintained static-JSON mirror at `brolesi.github.io/taco`)
for generic and prepared Brazilian foods -- each entry states its preparation state explicitly
(`cru`, `cozido`, etc), unlike a source that defaults to "as sold" for staples like rice -- and
Open Food Facts for Brazilian branded products (scoped to `countries_tags:"en:brazil"` and
`lang=pt`, so search matches the Portuguese names actual Brazilian packaging was scanned with).
Both speak `IExternalFoodSource`; the composite runs them in parallel and concatenates the
results, so a search shows the generic food and the specific packaged product side by side. One
source failing (e.g. a network error) degrades to just the other's results rather than failing
the whole search. `Food.Source` (`FoodSource` enum) records which one a cached food came from:
`Taco`, `OpenFoodFacts`, or `Custom`. `Usda` is a retired value kept only so a food cached from
an earlier USDA search still parses -- USDA is no longer queried live.

Custom foods can also be created, such as:

```text
My whey
My yogurt
Brand X rice
```

---

# 15. Units of measurement

Food and drinks are measured differently, and the app never mixes the two.

## Mass, for food

```text
g
kg
```

## Volume, for drinks

```text
ml
L
```

Each `Food` is either a food (measured by mass) or a drink (measured by volume); its base unit says which (section 16). There is no separate "food or drink" flag. The user logs a quantity in the food's own unit, and the app always offers grams for a food and milliliters for a drink, starting at 100.

There are no household measures (can, glass, cup, spoon). Knowing that a can holds 350 ml is the user's job; the app does not keep a catalog of containers.

Water is logged on its own (section 9) and never becomes a `Food`. Drinking a soda does not count toward the water goal.

---

# 16. Base nutritional unit

Each `Food` has a base unit: always 100, of grams for a food or of milliliters for a drink. Nutrition values are per that base unit.

Examples:

```text
Rice
130 kcal / 100g

Yogurt
61 kcal / 100g

Coca-Cola
42 kcal / 100ml
```

Where the base unit comes from:

- **Open Food Facts**: the unit in the product's package quantity (`"350ml"` → ml, `"500 g"` → g), or, when the quantity has no unit, ml for the `en:beverages` category except beverage preparations (`en:beverage-preparations`: coffee capsules, drink powders), which stay in g. Open Food Facts already reports a drink's nutrients per 100 ml (in fields still named `_100g`), so the values are used as they come.
- **TACO**: ml for the "Bebidas" category and for a fixed list of liquids filed elsewhere (juices, milks); g for everything else, yogurts included. TACO measures everything per 100 g, so for those drinks the values are read as per 100 ml. Drinks weigh 0.95 to 1.05 g per ml, so the error stays under 5%.
- **Custom foods**: the user picks "Comida" (g) or "Bebida" (ml), and can change it later. Changing it moves the amounts already logged for that food to the new unit, number unchanged (350 g becomes 350 ml), keeping their nutritional snapshot.

---

# 17. Servings (removed)

There are no named portions ("1 can = 350 ml"). A meal item is always a quantity in the food's own unit, grams or milliliters (section 15). An earlier `FoodServing` model was removed without ever holding data.

---

# 18. Conversion rule

Do not assume a universal conversion between mass and volume.

Example:

```text
100 ml of water ≈ 100g
```

But:

```text
100 ml of oil ≠ 100g
100 ml of flour ≠ 100g
```

Mass ↔ volume conversion depends on the food's density, so it never happens at calculation time:

- g/kg convert into each other, and ml/L convert into each other;
- logging grams of a drink, or milliliters of a food, is rejected with a 400;
- the only "100 g ≈ 100 ml" reading is the one-time import of TACO drinks described in section 16.

---

# 19. Nutritional calculation

Example:

Food:

```text
Rice
130 kcal / 100g
```

User:

```text
200g
```

Calculation:

```text
200 / 100 * 130 = 260 kcal
```

The same principle applies to:

- protein;
- carbohydrates;
- fat.

A drink works the same way, in milliliters:

```text
Coca-Cola
42 kcal / 100ml

350 ml
↓
350 / 100 * 42 = 147 kcal
```

The API must perform these calculations.

Water intake does not go through this calculation — it has no nutritional value, only a logged volume that is summed per day.

---

# 20. Daily targets

The user can set a desired calorie deficit.

Example:

```text
Estimated TDEE:      2500 kcal
Desired deficit:      500 kcal
Daily target:        2000 kcal
```

In the future the system may automatically calculate TDEE using:

```text
age
height
weight
biological sex
activity level
```

The specific formula can be defined during implementation. Standard BMR formulas (e.g. Mifflin-St Jeor) branch on biological sex, not gender identity — `User.gender` (Section 7) is not a direct input to this formula. If auto-calculated TDEE is built, it needs its own, separately-collected biological-sex input for the formula, decoupled from the `gender` field the user sees and controls elsewhere in the profile.

Alongside the calorie target, the user profile also carries optional daily goals for water, protein, carbohydrates and fat (Section 7). Water is still tracked as a running daily total regardless of whether a goal is set (Section 9); the goal is only used as a reference line on the dashboard (Section 33).

---

# 21. Dashboard

The dashboard will not be a persisted table/collection.

It is a view computed from existing data.

Sources:

```text
User
+
Meals
+
MealItems
+
WeightEntries
+
WaterEntries
```

The dashboard aggregates, per day, calories, macronutrients (protein, carbs, fat), and water intake — not calories alone.

Example:

```text
Date       Weight    Calories   Protein   Carbs   Fat    Water
-------------------------------------------------------------------
09/01      88.1      1920       140g      190g    55g    1800ml
09/02      87.9      2150       155g      210g    62g    2100ml
09/03      87.6      1720       130g      160g    48g    1500ml
09/04      —         1850       135g      175g    50g    2000ml
09/05      87.4      1970       145g      195g    58g    1750ml
```

This data feeds the charts.

---

# 22. Metrics and charts overview

Food Track tracks six chartable metrics over time:

```text
Weight
Calories
Protein
Carbs
Fat
Water
```

Each metric can be viewed:

- individually, as its own chart (Sections 23–26);
- combined with any other subset of metrics on a single chart (Section 27) — e.g. only calories, only protein, or calories + protein + water together.

All metrics share the same period filters (7 days, 30 days, 3 months, 6 months, 1 year, and in the future a custom period), so switching between an individual chart and a combined view keeps the same time range selected.

Because these metrics use different units and scales (kcal, grams, ml, kg), overlaying them raw on a single axis can be misleading. The exact rendering approach — separate axes, a normalized/percentage-of-target view, or several small charts stacked together — is a frontend/UX decision to make during implementation, not a backend concern. The backend's only responsibility is to expose each metric's history so the frontend can combine whichever subset the user selects.

---

# 23. Weight chart

Show:

- logged weight;
- progress over time;
- optionally a 7-day moving average.

The moving average can help visualize the trend and reduce the visual impact of daily fluctuations.

Filters:

```text
7 days
30 days
3 months
6 months
1 year
```

In the future:

```text
custom period
```

---

# 24. Calorie chart

Show per day:

- calories consumed;
- daily target.

Example:

```text
Target: 2000 kcal

09/01 → 1920
09/02 → 2150
09/03 → 1720
09/04 → 1850
```

This allows visualizing days above or below the target.

In the future:

- daily average;
- weekly average;
- estimated cumulative deficit/surplus;
- trend.

---

# 25. Macronutrient chart

Just like calories, protein, carbohydrates, and fat must be trackable as both data and charts — not only as numbers embedded inside a meal.

Show per day:

- protein consumed (g);
- carbohydrates consumed (g);
- fat consumed (g);
- optionally a reference line for each macronutrient target, if the user has configured one (Section 7).

Example:

```text
09/01 → Protein 140g | Carbs 190g | Fat 55g
09/02 → Protein 155g | Carbs 210g | Fat 62g
09/03 → Protein 130g | Carbs 160g | Fat 48g
09/04 → Protein 135g | Carbs 175g | Fat 50g
```

Suggested visualizations:

- one line/bar per macronutrient over time, using the same period filters as the calorie chart (7 days, 30 days, 3 months, 6 months, 1 year);
- a daily macronutrient breakdown (e.g. stacked bar or donut) showing the share of protein, carbs, and fat for the selected day.

The macronutrient chart follows the same filtering rules as the calorie chart (Section 24) so the user can compare periods consistently.

---

# 26. Water chart

Show per day:

- water consumed (ml or L);
- optionally a reference line for the daily water target, if the user has configured one (Section 7).

Example:

```text
09/01 → 1800 ml
09/02 → 2100 ml
09/03 → 1500 ml
09/04 → 2000 ml
```

Uses the same period filters as the other charts (7 days, 30 days, 3 months, 6 months, 1 year).

---

# 27. Combined metrics chart

The user can select two or more metrics — for example calories + protein, or weight + calories + water — and view them together on a single chart, over the same period filter.

Example selection:

```text
Selected metrics: Calories, Protein, Water
Period: 30 days
```

Rules:

- selecting a single metric is equivalent to viewing that metric's individual chart (Sections 23–26);
- there is no fixed limit on how many metrics can be combined — the user may select all six if useful, though usability may guide a sensible default (e.g. up to 3 preselected);
- the combined chart reuses the same per-metric history data returned by the dashboard endpoint (Section 33) — no separate persisted data or endpoint is needed for combinations.

This is purely a presentation feature: the backend always returns the full set of metric histories for the requested period, and the frontend decides which ones are currently selected and how to render them together (see Section 22 for rendering considerations).

---

# 28. Persisted vs. derived data

Fundamental principle:

> Persist facts, not visualizations.

Persist:

```text
Weight logged on a given date
Water intake logged at a given time
Meal that took place
Food consumed
Quantity consumed
Nutritional values
User's target
```

There is no need to persist:

```text
Chart
Weekly progression
Monthly progression
Yearly progression
Weekly average
Monthly average
Daily water total
```

This data can be derived from historical records.

---

# 29. Full day example

User logs:

```text
Weight:
87.6 kg

Target:
2000 kcal
```

Water:

```text
08:00 → 250 ml
11:30 → 300 ml
14:00 → 500 ml
19:00 → 250 ml
```

Breakfast:

```text
2 eggs
bread
cheese
```

Lunch:

```text
200g rice
150g chicken
100g beans
```

Snack:

```text
1/2 cup of yogurt
```

Dinner:

```text
...
```

Each meal contains its `MealItems`.

The backend calculates the meal's values.

Then aggregates:

```text
Breakfast
+
Lunch
+
Snack
+
Dinner
=
DailyCalories, DailyProtein, DailyCarbs, DailyFat

WaterEntries for the day
=
DailyWater
```

The dashboard can show:

```text
1,720 / 2,000 kcal
130g protein / 160g carbs / 48g fat
1,300 ml water

87.6 kg
```

---

# 30. Conceptual Firestore structure

A possible structure:

```text
users/
    {userId}

meals/
    {mealId}
        items/
            {itemId}

weightEntries/
    {entryId}

waterEntries/
    {entryId}

foods/
    {foodId}
```

The physical decision can be made during implementation.

What matters is keeping the conceptual entities:

```text
User
Meal
MealItem
WeightEntry
WaterEntry
Food
```

---

# 31. Relationship between user and data

Every entity directly belonging to a user must allow identifying its owner.

Example:

```text
User
id = U1

Meal
id = M1
userId = U1

WeightEntry
id = W1
userId = U1

WaterEntry
id = WA1
userId = U1
```

So:

```text
U1
├── M1
├── M2
├── M3
├── W1
├── W2
└── WA1
```

This will allow, in the future:

```text
U1 → their data
U2 → their data
U3 → their data
```

without remodeling the domain.

---

# 32. Conceptual endpoints

The API can evolve into something like:

```text
GET    /profile
PUT    /profile

GET    /foods/search?q=...
GET    /foods/{id}
POST   /foods

GET    /meals?from=...&to=...
GET    /meals/{id}
POST   /meals
PUT    /meals/{id}
DELETE /meals/{id}

GET    /weight-entries?from=...&to=...
POST   /weight-entries
PUT    /weight-entries/{id}
DELETE /weight-entries/{id}

GET    /water-entries?from=...&to=...
POST   /water-entries
PUT    /water-entries/{id}
DELETE /water-entries/{id}

GET    /dashboard?from=...&to=...
```

The exact contracts will be defined during implementation.

---

# 33. Dashboard API

There can be a dedicated endpoint providing aggregated dashboard data:

```text
GET /dashboard?from=2026-09-01&to=2026-09-30
```

Conceptual response:

```json
{
  "dailyCalorieTarget": 2000,
  "currentWeightKilograms": 87.6,
  "targetWeightKilograms": 80,
  "dailyWaterTargetMilliliters": 4000,
  "dailyProteinTargetGrams": 140,
  "dailyCarbsTargetGrams": 190,
  "dailyFatTargetGrams": 55,
  "weightHistory": [
    {
      "date": "2026-09-01",
      "weightKilograms": 88.1
    },
    {
      "date": "2026-09-03",
      "weightKilograms": 87.6
    }
  ],
  "nutritionHistory": [
    {
      "date": "2026-09-01",
      "calories": 1920,
      "protein": 140,
      "carbs": 190,
      "fat": 55,
      "waterMilliliters": 1800
    },
    {
      "date": "2026-09-02",
      "calories": 2150,
      "protein": 155,
      "carbs": 210,
      "fat": 62,
      "waterMilliliters": 2100
    }
  ]
}
```

The `dailyCalorieTarget`, `targetWeightKilograms`, `dailyWaterTargetMilliliters`, `dailyProteinTargetGrams`, `dailyCarbsTargetGrams` and `dailyFatTargetGrams` fields mirror the user's profile goals (Section 7, all optional except `dailyCalorieTarget`) and let the frontend draw a reference line per metric.

`currentWeightKilograms` is the most recent weight entry the user has logged overall, regardless of whether it falls inside `from`/`to` — it answers "what do I weigh right now," not "what did I weigh during this chart's window."

`nutritionHistory` combines calories, macros and water into one array because they are always computed together from the same day's meals and water entries. It contains exactly one entry per calendar day in `[from, to]`, zero-filled for days with no meals or water logged, so the frontend can render a continuous line without filling gaps itself.

`weightHistory`, in contrast, stays sparse — only days with an actual weight entry — since weight is not expected to be logged daily (Section 8) and a fabricated zero would be misleading.

The frontend remains mainly responsible for presentation, including deciding which metrics to render individually or combined (Section 27).

---

# 34. Initial UX

Dashboard:

```text
┌───────────────────────────────┐
│          DASHBOARD            │
│                               │
│ Current weight  87.6 kg       │
│ Target          80.0 kg       │
│                               │
│ Calories today  1720 / 2000   │
│ Protein today   130g          │
│ Carbs today     160g          │
│ Fat today       48g           │
│ Water today     1300 ml       │
│                               │
│ ───────────────────────────── │
│                               │
│ Charts        [Individual|Combined] │
│                               │
│ Weight                         │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Calories                       │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Macronutrients                 │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Water                          │
│       chart                   │
│                               │
└───────────────────────────────┘
```

A metric picker lets the user switch any chart between an individual metric and a combination of two or more (Section 27) — e.g. tapping "Calories" and "Protein" together renders one combined chart instead of two separate ones.

Quick actions:

```text
+ Log meal
+ Log weight
+ Log water
```

---

# 35. MVP features

## Do

- React Native.
- Expo.
- TypeScript.
- .NET API.
- Firebase Authentication.
- Firestore.
- Firebase Admin SDK.
- Integration with external nutritional API.
- Food cache in Firestore.
- User profile.
- Daily calorie target.
- Meal logging.
- MealItem logging.
- Calorie/macronutrient calculation.
- Optional weight logging.
- Weight history.
- Water logging.
- Water history.
- Dashboard.
- Weight chart.
- Calorie chart.
- Macronutrient chart (protein, carbs, fat).
- Water chart.
- Combined/selectable metrics chart (view any subset of weight, calories, protein, carbs, fat, and water together).
- Daily totals for calories, protein, carbs, fat, and water.
- Period filters.

## Not initially

- Professional nutrition system.
- Wearable integration.
- AI-based food recognition.
- Portion estimation from photos.
- Social features.
- Sharing.
- Advanced administration.
- Premature optimizations.
- Overly complex architecture.

---

# 36. Future features

Possibilities:

- Social login.
- Full multi-user support.
- Favorite meals.
- "Add my usual lunch".
- Custom foods.
- Barcode scanner.
- Notifications/reminders (e.g. water intake reminders).
- Offline support + sync.
- Weight moving average.
- Estimated rate of loss.
- Target history.
- Companion PWA/web app.
- Sharing between users.

---

# 37. Recommended implementation order

## Phase 1 — Foundation

1. Create React Native + Expo + TypeScript project.
2. Create .NET Web API.
3. Configure Firebase.
4. Configure Firebase Authentication.
5. Configure Firestore.
6. Configure Firebase Admin SDK on the backend.

## Phase 2 — Profile

7. Create User/Profile.
8. Create profile read/update.
9. Configure daily calorie target.

## Phase 3 — Weight & Water

10. Create WeightEntry.
11. Create weight endpoints.
12. Create weight logging screen.
13. Create weight history view.
14. Create WaterEntry.
15. Create water endpoints.
16. Create water logging screen.
17. Include water totals in history/dashboard aggregation.

## Phase 4 — Foods

18. Create Food.
19. Integrate external nutritional API.
20. Implement cache.
21. Measure each food in g or ml (named servings were dropped).
22. Implement unit conversion.

## Phase 5 — Meals

23. Create Meal.
24. Create MealItem.
25. Implement nutritional calculation.
26. Implement nutritional snapshot.
27. Create meal logging screen.

## Phase 6 — Dashboard

28. Create dashboard endpoint.
29. Aggregate calories, macronutrients, and water per day.
30. Fetch weight history.
31. Create weight chart.
32. Create calorie chart.
33. Create macronutrient chart.
34. Create water chart.
35. Implement combined/selectable metrics chart.
36. Add period filters.

## Phase 7 — Refinement

37. Improve mobile UX.
38. Create logging shortcuts.
39. Add error handling.
40. Add loading/empty states.
41. Add validations.
42. Add key tests.

---

# 38. Architectural principles

1. The frontend does not access Firestore directly.
2. The .NET API is the domain boundary.
3. The backend identifies the user from the authenticated token.
4. Never trust a `userId` sent by the frontend.
5. Entities belonging to a user carry a `userId`.
6. User IDs and entity IDs are independent.
7. `MealId` identifies a meal.
8. `UserId` identifies the owner.
9. `Food` represents nutritional information/cache.
10. `MealItem` stores a nutritional snapshot.
11. Mass and volume are different units.
12. A food is logged in its own unit: grams for food, milliliters for drinks.
13. Persist historical facts.
14. Derive charts and aggregations — including calorie, macronutrient (protein, carbs, fat), and water totals and charts, viewable individually or combined.
15. Avoid unnecessary complexity in the MVP.
16. Keep the domain ready for growth without building features that have no value yet.

---

# 39. Final architectural summary

```text
React Native + Expo
        |
        | HTTPS / JSON
        v
     .NET API
        |
        +---------------------+
        |                     |
        v                     v
Firebase Auth            Firestore
                              |
                              +-- Users
                              +-- Meals
                              +-- MealItems
                              +-- WeightEntries
                              +-- WaterEntries
                              +-- Foods

                              +
                              |
                              v
                       External Food API
```

Firebase provides the managed infrastructure.

Firebase Authentication provides authentication.

Firestore provides persistence.

The .NET API concentrates the domain, authorization, calculations, and integrations.

The React Native + Expo frontend provides the mobile experience.

The database stores facts:

```text
"I ate this."
"I weighed this."
"I drank this."
"This food has these nutritional values."
"My target is this."
```

The dashboard transforms these facts into:

```text
calories per day
protein, carbs, and fat per day
water intake per day
average calories, macronutrients, and water
estimated deficit/surplus
weight over time
weight trend
progress toward the target
individual or combined metric charts
```

The first version must remain small, functional, and easy to evolve.

The architecture should not anticipate unnecessary complexity, but it also should not create a dead end for authentication, multi-user support, custom foods, favorite meals, notifications, barcode scanning, or offline support.
