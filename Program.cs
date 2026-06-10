using Microsoft.EntityFrameworkCore;
using CanaryTracker.Data;
using CanaryTracker.Services;
using CanaryTracker.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHostedService<CanaryWatcherService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapStatsEndpoints();
app.MapCanaryEndpoints();
app.MapAlertEndpoints();

app.Run();
