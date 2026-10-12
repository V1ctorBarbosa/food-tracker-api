using Amazon.Lambda.AspNetCoreServer.Hosting;
using FoodTrack.Api.Authentication;
using FoodTrack.Api.Endpoints;
using FoodTrack.Api.ErrorHandling;
using FoodTrack.Api.RateLimiting;
using FoodTrack.Application;
using FoodTrack.Infrastructure;
using FoodTrack.Infrastructure.Configuration;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;

const string DevelopmentCorsPolicyName = "DevelopmentCors";
const string ExpoWebDevServerOrigin = "http://localhost:8081";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddParameterStoreWhenConfigured();

// A no-op outside Lambda -- Kestrel keeps serving requests exactly as today for local dev.
// Only takes over when this same executable is actually invoked as a Lambda function.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

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

builder.Services.AddUserSessionStatus();
builder.Services
    .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(FirebaseAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddPerUserRateLimiting();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevelopmentCorsPolicyName);
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
// After authentication, so requests are counted per verified user rather than per caller-supplied value.
app.UseRateLimiter();

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
.WithName("GetFirebaseHealth")
.RequireAuthorization();

app.MapProfileEndpoints();
app.MapAccountEndpoints();
app.MapWeightEntryEndpoints();
app.MapWaterEntryEndpoints();
app.MapFoodEndpoints();
app.MapMealEndpoints();
app.MapMealEstimateEndpoints();
app.MapDashboardEndpoints();

app.Run();
