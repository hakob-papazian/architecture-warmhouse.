namespace DeviceManagementService;

public record CreateDeviceRequest(Guid HouseId, string TypeName, string SerialNumber, string Name, string Location);
public record UpdateDeviceStatusRequest(string Status);

public record DeviceResponse(
    Guid Id,
    Guid HouseId,
    string TypeName,
    string SerialNumber,
    string Name,
    string Location,
    string Status,
    DateTimeOffset CreatedAt);

public record CreateModuleRequest(string ModuleType, string Name);
public record ModuleResponse(Guid Id, Guid DeviceId, string ModuleType, string Name, string Status);
