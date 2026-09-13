# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder
WORKDIR /src

COPY Smarthome.Shared/Smarthome.Shared.csproj Smarthome.Shared/
COPY UserHomeService/UserHomeService.csproj UserHomeService/
RUN dotnet restore UserHomeService/UserHomeService.csproj

COPY Smarthome.Shared/ Smarthome.Shared/
COPY UserHomeService/ UserHomeService/
RUN dotnet publish UserHomeService/UserHomeService.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=builder /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "UserHomeService.dll"]
