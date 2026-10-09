using FoodTrack.Application.Foods;
using FoodTrack.Application.Meals;
using FoodTrack.Infrastructure.Firebase;
using FoodTrack.Infrastructure.Foods;
using FoodTrack.Infrastructure.Meals;
using FoodTrack.Migrations.BaseUnits;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Usage, from api/:
//   dotnet run --project src/FoodTrack.Migrations -- --Firebase:ProjectId <id> --Firebase:CredentialsPath <key.json>
// prints what would change; add "--Apply true" to write it.

const string ApplyKey = "Apply";
const string TacoBaseUrl = "https://brolesi.github.io/taco/";
const string OpenFoodFactsBaseUrl = "https://world.openfoodfacts.org/";
const string OpenFoodFactsUserAgent = "FoodTrack-Migrations/1.0";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddFirebaseAdmin(builder.Configuration);
builder.Services.AddScoped<IFoodRepository, FirestoreFoodRepository>();
builder.Services.AddScoped<IMealRepository, FirestoreMealRepository>();
builder.Services.AddHttpClient<TacoFoodUnitClassifier>(client => client.BaseAddress = new Uri(TacoBaseUrl));
builder.Services.AddHttpClient<OpenFoodFactsFoodUnitClassifier>(client =>
{
    client.BaseAddress = new Uri(OpenFoodFactsBaseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(OpenFoodFactsUserAgent);
});
builder.Services.AddScoped<IFoodUnitClassifier>(services => services.GetRequiredService<TacoFoodUnitClassifier>());
builder.Services.AddScoped<IFoodUnitClassifier>(services => services.GetRequiredService<OpenFoodFactsFoodUnitClassifier>());
builder.Services.AddScoped<BaseUnitBackfill>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var applyChanges = builder.Configuration.GetValue<bool>(ApplyKey);
var backfill = scope.ServiceProvider.GetRequiredService<BaseUnitBackfill>();
var report = await backfill.RunAsync(applyChanges);

Console.WriteLine(BaseUnitBackfillReportFormatter.Format(report, applyChanges));
