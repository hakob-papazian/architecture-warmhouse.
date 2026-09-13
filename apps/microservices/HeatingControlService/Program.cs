using HeatingControlService;
using HeatingControlService.Consumers;
using HeatingControlService.Data;
using HeatingControlService.Models;
using HeatingControlService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Smarthome.Shared.Auth;
using Smarthome.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/heatingcontrol");
var connectionString = Smarthome.Shared.Data.PostgresConnectionStringBuilder.FromUrl(databaseUrl);
builder.Services.AddDbContext<HeatingDbContext>(options => options.UseNpgsql(connectionString));

var userHomeServiceUrl = GetEnv("USER_HOME_SERVICE_URL", "http://user-home-service:8080");
builder.Services.AddHttpClient<HouseDirectoryClient>(client => client.BaseAddress = new Uri(userHomeServiceUrl));

builder.Services.AddScoped<IHeatingEquipmentClient, SimulatedHeatingEquipmentClient>();
builder.Services.AddScoped<HeatingCommandDispatcher>();

builder.Services.AddSingleton(RabbitMqOptions.FromEnvironment());
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<TemperatureUpdatedConsumer>();

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
    scope.ServiceProvider.GetRequiredService<HeatingDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

static IResult HouseNotFoundResult(Guid houseId) => Results.NotFound(new { error = "not_found", message = $"House {houseId} not found" });

app.MapGet("/houses/{houseId:guid}/heating", async (Guid houseId, HeatingCommandDispatcher dispatcher, CancellationToken ct) =>
{
    var profile = await dispatcher.GetOrCreateProfileAsync(houseId, ct);
    return profile is null ? HouseNotFoundResult(houseId) : Results.Ok(HeatingProfileResponse.From(profile));
}).RequireAuthorization();

app.MapPatch("/houses/{houseId:guid}/heating/target-temperature", async (
    Guid houseId, UpdateTargetTemperatureRequest req, HeatingDbContext db, HeatingCommandDispatcher dispatcher, CancellationToken ct) =>
{
    if (req.TargetTemperature is < 5 or > 30)
    {
        return Results.BadRequest(new { error = "validation_error", message = "targetTemperature must be between 5 and 30" });
    }

    var profile = await dispatcher.GetOrCreateProfileAsync(houseId, ct);
    if (profile is null)
    {
        return HouseNotFoundResult(houseId);
    }

    profile.TargetTemperature = req.TargetTemperature;
    profile.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);

    return Results.Ok(HeatingProfileResponse.From(profile));
}).RequireAuthorization();

app.MapPatch("/houses/{houseId:guid}/heating/mode", async (
    Guid houseId, UpdateModeRequest req, HeatingDbContext db, HeatingCommandDispatcher dispatcher, CancellationToken ct) =>
{
    if (!Enum.TryParse<HeatingMode>(req.Mode, ignoreCase: true, out var mode))
    {
        return Results.BadRequest(new { error = "validation_error", message = "mode must be 'manual' or 'auto'" });
    }

    var profile = await dispatcher.GetOrCreateProfileAsync(houseId, ct);
    if (profile is null)
    {
        return HouseNotFoundResult(houseId);
    }

    profile.Mode = mode;
    profile.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);

    return Results.Ok(HeatingProfileResponse.From(profile));
}).RequireAuthorization();

app.MapPost("/houses/{houseId:guid}/heating/commands", async (
    Guid houseId, SendCommandRequest req, HeatingCommandDispatcher dispatcher, CancellationToken ct) =>
{
    if (!Enum.TryParse<HeatingState>(req.Action, ignoreCase: true, out var action))
    {
        return Results.BadRequest(new { error = "validation_error", message = "action must be 'on' or 'off'" });
    }

    var (outcome, command) = await dispatcher.DispatchAsync(houseId, action, CommandSource.Manual, ct);

    return outcome switch
    {
        DispatchOutcome.Applied => Results.Created($"/houses/{houseId}/heating/commands/{command!.Id}", HeatingCommandResponse.From(command)),
        DispatchOutcome.Conflict => Results.Json(
            new { error = "conflict", message = "House is in auto mode; switch to manual before sending manual commands" },
            statusCode: StatusCodes.Status409Conflict),
        DispatchOutcome.EquipmentUnreachable => Results.Json(
            new { error = "equipment_unreachable", message = "Heating equipment did not acknowledge the command" },
            statusCode: StatusCodes.Status502BadGateway),
        DispatchOutcome.HouseNotFound => HouseNotFoundResult(houseId),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization();

app.MapGet("/houses/{houseId:guid}/heating/commands", async (
    Guid houseId, int? limit, HeatingDbContext db, HouseDirectoryClient houseDirectory, CancellationToken ct) =>
{
    if (!await houseDirectory.HouseExistsAsync(houseId, ct))
    {
        return HouseNotFoundResult(houseId);
    }

    var commands = await db.Commands
        .Where(c => c.HouseId == houseId)
        .OrderByDescending(c => c.IssuedAt)
        .Take(limit ?? 20)
        .ToListAsync(ct);

    return Results.Ok(new { items = commands.Select(HeatingCommandResponse.From), nextCursor = (string?)null });
}).RequireAuthorization();

app.Run();
