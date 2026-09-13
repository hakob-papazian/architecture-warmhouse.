namespace Smarthome.Shared.Data;

public static class PostgresConnectionStringBuilder
{
    // Converts a "postgres://user:pass@host:port/dbname" URL into an Npgsql
    // connection string. Mirrors SmartHome.Api.Data.PostgresConnectionStringBuilder
    // (apps/smart_home-dotnet) so DATABASE_URL has the same shape everywhere.
    public static string FromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port == -1 ? 5432 : uri.Port;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password}";
    }
}
