using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Smarthome.Shared.Auth;
using UserHomeService;
using UserHomeService.Data;
using UserHomeService.Models;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var databaseUrl = GetEnv("DATABASE_URL", "postgres://postgres:postgres@localhost:5432/userhome");
var connectionString = Smarthome.Shared.Data.PostgresConnectionStringBuilder.FromUrl(databaseUrl);

builder.Services.AddDbContext<UserHomeDbContext>(options => options.UseNpgsql(connectionString));

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
    scope.ServiceProvider.GetRequiredService<UserHomeDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/auth/register", async (RegisterRequest req, UserHomeDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { error = "email and password are required" });
    }

    if (await db.Users.AnyAsync(u => u.Email == req.Email, ct))
    {
        return Results.Conflict(new { error = "a user with this email already exists" });
    }

    var user = new User
    {
        Name = req.Name,
        Email = req.Email,
        PasswordHash = PasswordHasher.Hash(req.Password),
    };
    db.Users.Add(user);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/users/{user.Id}", new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt));
});

app.MapPost("/auth/login", async (LoginRequest req, UserHomeDbContext db, CancellationToken ct) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email, ct);
    if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateJwtSecurityToken(
        issuer: JwtSettings.Issuer,
        audience: JwtSettings.Audience,
        subject: new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
        }),
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(JwtSettings.GetSecurityKey(), SecurityAlgorithms.HmacSha256));

    return Results.Ok(new LoginResponse(tokenHandler.WriteToken(token), user.Id));
});

app.MapGet("/homes", async (ClaimsPrincipal principal, UserHomeDbContext db, CancellationToken ct) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var houses = await db.Houses.Where(h => h.OwnerUserId == userId).ToListAsync(ct);
    return Results.Ok(houses.Select(h => new HouseResponse(h.Id, h.OwnerUserId, h.Name, h.Address, h.CreatedAt)));
}).RequireAuthorization();

app.MapPost("/homes", async (CreateHouseRequest req, ClaimsPrincipal principal, UserHomeDbContext db, CancellationToken ct) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var house = new House { OwnerUserId = userId, Name = req.Name, Address = req.Address };
    db.Houses.Add(house);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/homes/{house.Id}", new HouseResponse(house.Id, house.OwnerUserId, house.Name, house.Address, house.CreatedAt));
}).RequireAuthorization();

// Internal, service-to-service only endpoints (reached directly over the compose
// network, never through the API Gateway) backing the Access Checker component.
app.MapGet("/internal/access", async (Guid userId, Guid houseId, UserHomeDbContext db, CancellationToken ct) =>
{
    var hasAccess = await db.Houses.AnyAsync(h => h.Id == houseId && h.OwnerUserId == userId, ct);
    return Results.Ok(new AccessCheckResponse(hasAccess));
});

app.MapGet("/internal/houses/{houseId:guid}/owner", async (Guid houseId, UserHomeDbContext db, CancellationToken ct) =>
{
    var house = await db.Houses.FindAsync([houseId], ct);
    return house is null ? Results.NotFound() : Results.Ok(new HouseOwnerResponse(house.OwnerUserId));
});

app.Run();
