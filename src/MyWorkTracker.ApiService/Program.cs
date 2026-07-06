using Microsoft.EntityFrameworkCore;
using MyWorkTracker.ApiService.Data;
using MyWorkTracker.ApiService.Endpoints;
using MyWorkTracker.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddSingleton<PrioritizationService>();
builder.Services.AddScoped<AzureDevOpsSyncService>();

builder.AddNpgsqlDbContext<TrackerDbContext>("trackerdb");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Auto-migrate in development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TrackerDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

// Map all API endpoints
app.MapRepoEndpoints();
app.MapPrEndpoints();
app.MapWorkItemEndpoints();
app.MapEpicEndpoints();
app.MapAgentEndpoints();

app.Run();
