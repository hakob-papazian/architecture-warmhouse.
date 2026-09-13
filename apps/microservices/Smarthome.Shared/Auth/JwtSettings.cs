using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Smarthome.Shared.Auth;

/// <summary>
/// The signing key is shared (via the JWT_SIGNING_KEY env var) between User & Home
/// Service (issues tokens) and API Gateway (validates them) so the gateway can check
/// a token statelessly, without calling back into User & Home Service per request.
/// </summary>
public static class JwtSettings
{
    public const string Issuer = "smarthome-user-home-service";
    public const string Audience = "smarthome-microservices";

    public static string GetSigningKey() =>
        Environment.GetEnvironmentVariable("JWT_SIGNING_KEY")
        ?? "dev-only-signing-key-change-me-please-32chars-min";

    public static SymmetricSecurityKey GetSecurityKey() =>
        new(Encoding.UTF8.GetBytes(GetSigningKey()));
}
