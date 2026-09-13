using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationService;
using NotificationService.Consumers;
using NotificationService.Data;
using Smarthome.Shared.Auth;
using Smarthome.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/notification");
var connectionString = Smarthome.Shared.Data.PostgresConnectionStringBuilder.FromUrl(databaseUrl);
builder.Services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));

var userHomeServiceUrl = GetEnv("USER_HOME_SERVICE_URL", "http://user-home-service:8080");
builder.Services.AddHttpClient<HouseOwnerClient>(client => client.BaseAddress = new Uri(userHomeServiceUrl));

builder.Services.AddSingleton(RabbitMqOptions.FromEnvironment());
builder.Services.AddHostedService<TemperatureUpdatedConsumer>();
builder.Services.AddHostedService<HeatingStateChangedConsumer>();

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
    scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/users/{userId:guid}/notifications", async (Guid userId, NotificationDbContext db, CancellationToken ct) =>
{
    var logs = await db.Logs
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.SentAt)
        .ToListAsync(ct);

    return Results.Ok(logs);
}).RequireAuthorization();

app.Run();
