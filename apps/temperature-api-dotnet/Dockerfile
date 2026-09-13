# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder
WORKDIR /src

# Copy csproj and restore dependencies
COPY temperature-api-dotnet.csproj ./
RUN dotnet restore

# Copy the source code and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=builder /app .

EXPOSE 8081

ENTRYPOINT ["dotnet", "TemperatureApi.dll"]
