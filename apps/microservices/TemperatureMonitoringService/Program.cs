using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Smarthome.Shared.Auth;
using Smarthome.Shared.Messaging;
using TemperatureMonitoringService;
using TemperatureMonitoringService.Data;
using TemperatureMonitoringService.Models;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/temperaturemonitoring");
var connectionString = Smarthome.Shared.Data.PostgresConnectionStringBuilder.FromUrl(databaseUrl);
builder.Services.AddDbContext<TemperatureDbContext>(options => options.UseNpgsql(connectionString));

var sensorsApiUrl = GetEnv("TEMPERATURE_SENSORS_API_URL", "http://temperature-sensors-api:8081");
builder.Services.AddHttpClient<TemperatureSensorsApiClient>(client => client.BaseAddress = new Uri(sensorsApiUrl));

builder.Services.AddSingleton(RabbitMqOptions.FromEnvironment());
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<SensorPollerHostedService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtSettings.GetSecurityKey(),
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var port = GetEnv("PORT", "8080").TrimStart(':');
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TemperatureDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/houses/{houseId:guid}/temperature/watches", async (Guid houseId, WatchSensorRequest req, TemperatureDbContext db, CancellationToken ct) =>
{
    db.WatchedSensors.Add(new WatchedSensor { HouseId = houseId, DeviceId = req.DeviceId, SensorId = req.SensorId });
    await db.SaveChangesAsync(ct);
    return Results.Created($"/houses/{houseId}/temperature/watches", new { message = "watch registered" });
}).RequireAuthorization();

app.MapGet("/houses/{houseId:guid}/temperature", async (Guid houseId, TemperatureDbContext db, CancellationToken ct) =>
{
    var latest = await db.Readings
        .Where(r => r.HouseId == houseId)
        .OrderByDescending(r => r.RecordedAt)
        .FirstOrDefaultAsync(ct);

    return latest is null
        ? Results.NotFound(new { error = "no readings yet for this house" })
        : Results.Ok(new TelemetryResponse(latest.DeviceId, latest.Value, latest.Unit, latest.RecordedAt));
}).RequireAuthorization();

app.MapGet("/houses/{houseId:guid}/temperature/history", async (Guid houseId, int? limit, TemperatureDbContext db, CancellationToken ct) =>
{
    var readings = await db.Readings
        .Where(r => r.HouseId == houseId)
        .OrderByDescending(r => r.RecordedAt)
        .Take(limit ?? 20)
        .ToListAsync(ct);

    return Results.Ok(readings.Select(r => new TelemetryResponse(r.DeviceId, r.Value, r.Unit, r.RecordedAt)));
}).RequireAuthorization();

app.Run();
