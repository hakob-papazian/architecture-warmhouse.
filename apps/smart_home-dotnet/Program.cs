using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.Services;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

// Database
var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/smarthome");
var connectionString = PostgresConnectionStringBuilder.FromUrl(databaseUrl);

builder.Services.AddDbContext<SmartHomeDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ISensorRepository, SensorRepository>();

// Temperature service (talks to the external temperature-api microservice)
var temperatureApiUrl = GetEnv("TEMPERATURE_API_URL", "http://temperature-api:8081");
builder.Services.AddHttpClient<ITemperatureService, TemperatureService>(client =>
{
    client.BaseAddress = new Uri(temperatureApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Listen on the port from PORT (Go used ":8080"-style values; accept either form)
var port = GetEnv("PORT", "8080").TrimStart(':');
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
    db.Database.Migrate();
}

app.Logger.LogInformation("Connected to database successfully");
app.Logger.LogInformation("Temperature service initialized with API URL: {TemperatureApiUrl}", temperatureApiUrl);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Logger.LogInformation("Server starting on port {Port}", port);
app.Run();
