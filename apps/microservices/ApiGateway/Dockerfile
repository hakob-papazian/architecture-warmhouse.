# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder
WORKDIR /src

COPY Smarthome.Shared/Smarthome.Shared.csproj Smarthome.Shared/
COPY ApiGateway/ApiGateway.csproj ApiGateway/
RUN dotnet restore ApiGateway/ApiGateway.csproj

COPY Smarthome.Shared/ Smarthome.Shared/
COPY ApiGateway/ ApiGateway/
RUN dotnet publish ApiGateway/ApiGateway.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=builder /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
