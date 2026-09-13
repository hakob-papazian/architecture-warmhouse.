namespace UserHomeService;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, Guid UserId);
public record UserResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);
public record CreateHouseRequest(string Name, string Address);
public record HouseResponse(Guid Id, Guid OwnerUserId, string Name, string Address, DateTimeOffset CreatedAt);
public record AccessCheckResponse(bool HasAccess);
public record HouseOwnerResponse(Guid UserId);
