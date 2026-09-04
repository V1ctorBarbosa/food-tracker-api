using FoodTrack.Api.Authentication;
using FoodTrack.Api.Endpoints;
using FoodTrack.Application;
using FoodTrack.Infrastructure;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;

const string DevelopmentCorsPolicyName = "DevelopmentCors";
const string ExpoWebDevServerOrigin = "http://localhost:8081";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicyName, policy =>
    {
        policy
            .WithOrigins(ExpoWebDevServerOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(FirebaseAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevelopmentCorsPolicyName);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth");

app.MapGet("/health/firebase", async (FirestoreDb firestoreDb) =>
{
    var snapshot = await firestoreDb.Collection("_health").Document("ping").GetSnapshotAsync();
    return Results.Ok(new
    {
        status = "connected",
        projectId = firestoreDb.ProjectId,
        documentExists = snapshot.Exists,
    });
})
.WithName("GetFirebaseHealth");

app.MapProfileEndpoints();
app.MapWeightEntryEndpoints();
app.MapWaterEntryEndpoints();
app.MapFoodEndpoints();
app.MapMealEndpoints();
app.MapDashboardEndpoints();

app.Run();
