# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder
WORKDIR /src

COPY Smarthome.Shared/Smarthome.Shared.csproj Smarthome.Shared/
COPY DeviceManagementService/DeviceManagementService.csproj DeviceManagementService/
RUN dotnet restore DeviceManagementService/DeviceManagementService.csproj

COPY Smarthome.Shared/ Smarthome.Shared/
COPY DeviceManagementService/ DeviceManagementService/
RUN dotnet publish DeviceManagementService/DeviceManagementService.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=builder /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeviceManagementService.dll"]
