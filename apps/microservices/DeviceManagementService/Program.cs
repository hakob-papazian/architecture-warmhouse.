using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeviceManagementService;
using DeviceManagementService.Data;
using DeviceManagementService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Smarthome.Shared.Auth;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/devicemanagement");
var connectionString = Smarthome.Shared.Data.PostgresConnectionStringBuilder.FromUrl(databaseUrl);
builder.Services.AddDbContext<DeviceDbContext>(options => options.UseNpgsql(connectionString));

var userHomeServiceUrl = GetEnv("USER_HOME_SERVICE_URL", "http://user-home-service:8080");
builder.Services.AddHttpClient<AccessCheckClient>(client => client.BaseAddress = new Uri(userHomeServiceUrl));

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
    var db = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();
    db.Database.Migrate();

    if (!db.DeviceTypes.Any())
    {
        db.DeviceTypes.AddRange(
            new DeviceType { Name = "temperature_sensor", Category = "sensor", Unit = "C" },
            new DeviceType { Name = "heating_actuator", Category = "actuator" });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

static DeviceResponse ToResponse(Device d) => new(
    d.Id, d.HouseId, d.Type?.Name ?? string.Empty, d.SerialNumber, d.Name, d.Location, d.Status, d.CreatedAt);

// Reads are not access-checked per device (a documented simplification for this
// reference implementation) - only the mutating create path below verifies
// house ownership, matching the Command Handler -> User & Home Service arrow
// on the Device Management component diagram.
app.MapGet("/devices", async (Guid houseId, DeviceDbContext db, CancellationToken ct) =>
{
    var devices = await db.Devices.Include(d => d.Type).Where(d => d.HouseId == houseId).ToListAsync(ct);
    return Results.Ok(devices.Select(ToResponse));
}).RequireAuthorization();

app.MapGet("/devices/{id:guid}", async (Guid id, DeviceDbContext db, CancellationToken ct) =>
{
    var device = await db.Devices.Include(d => d.Type).FirstOrDefaultAsync(d => d.Id == id, ct);
    return device is null ? Results.NotFound(new { error = "device not found" }) : Results.Ok(ToResponse(device));
}).RequireAuthorization();

app.MapPost("/devices", async (
    CreateDeviceRequest req,
    ClaimsPrincipal principal,
    AccessCheckClient accessCheck,
    DeviceDbContext db,
    CancellationToken ct) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    if (!await accessCheck.HasAccessAsync(userId, req.HouseId, ct))
    {
        return Results.Json(new { error = "house does not belong to the current user" }, statusCode: StatusCodes.Status403Forbidden);
    }

    var type = await db.DeviceTypes.SingleOrDefaultAsync(t => t.Name == req.TypeName, ct);
    if (type is null)
    {
        return Results.BadRequest(new { error = $"unknown device type '{req.TypeName}'" });
    }

    var device = new Device
    {
        HouseId = req.HouseId,
        TypeId = type.Id,
        SerialNumber = req.SerialNumber,
        Name = req.Name,
        Location = req.Location,
    };
    db.Devices.Add(device);
    await db.SaveChangesAsync(ct);
    device.Type = type;

    return Results.Created($"/devices/{device.Id}", ToResponse(device));
}).RequireAuthorization();

app.MapPatch("/devices/{id:guid}/status", async (Guid id, UpdateDeviceStatusRequest req, DeviceDbContext db, CancellationToken ct) =>
{
    var device = await db.Devices.FindAsync([id], ct);
    if (device is null)
    {
        return Results.NotFound(new { error = "device not found" });
    }

    device.Status = req.Status;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { message = "status updated" });
}).RequireAuthorization();

app.MapDelete("/devices/{id:guid}", async (Guid id, DeviceDbContext db, CancellationToken ct) =>
{
    var device = await db.Devices.FindAsync([id], ct);
    if (device is null)
    {
        return Results.NotFound(new { error = "device not found" });
    }

    db.Devices.Remove(device);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { message = "device deleted" });
}).RequireAuthorization();

static ModuleResponse ToModuleResponse(Module m) => new(m.Id, m.DeviceId, m.ModuleType, m.Name, m.Status);

app.MapGet("/devices/{deviceId:guid}/modules", async (Guid deviceId, DeviceDbContext db, CancellationToken ct) =>
{
    var modules = await db.Modules.Where(m => m.DeviceId == deviceId).ToListAsync(ct);
    return Results.Ok(modules.Select(ToModuleResponse));
}).RequireAuthorization();

app.MapPost("/devices/{deviceId:guid}/modules", async (Guid deviceId, CreateModuleRequest req, DeviceDbContext db, CancellationToken ct) =>
{
    if (!await db.Devices.AnyAsync(d => d.Id == deviceId, ct))
    {
        return Results.NotFound(new { error = "device not found" });
    }

    var module = new Module { DeviceId = deviceId, ModuleType = req.ModuleType, Name = req.Name };
    db.Modules.Add(module);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/devices/{deviceId}/modules/{module.Id}", ToModuleResponse(module));
}).RequireAuthorization();

app.MapDelete("/devices/{deviceId:guid}/modules/{moduleId:guid}", async (Guid deviceId, Guid moduleId, DeviceDbContext db, CancellationToken ct) =>
{
    var module = await db.Modules.SingleOrDefaultAsync(m => m.Id == moduleId && m.DeviceId == deviceId, ct);
    if (module is null)
    {
        return Results.NotFound(new { error = "module not found" });
    }

    db.Modules.Remove(module);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { message = "module deleted" });
}).RequireAuthorization();

app.Run();
