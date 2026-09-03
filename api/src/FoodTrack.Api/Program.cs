using FoodTrack.Infrastructure.Firebase;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddFirebaseAdmin(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.Run();
