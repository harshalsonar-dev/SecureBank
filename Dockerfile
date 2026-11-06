# =========================================================================
# Multi-stage Dockerfile for SecureBank ASP.NET Core Enterprise Application
# Optimized for Azure Container Apps, AWS ECS/AppRunner, Render, and Docker Compose
# =========================================================================

# Stage 1: Build & Restore Environment
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies separately for layer caching
COPY ["SecureBank.csproj", "./"]
RUN dotnet restore "SecureBank.csproj"

# Copy the entire source code
COPY . .
RUN dotnet build "SecureBank.csproj" -c Release -o /app/build

# Stage 2: Publish optimized binaries
FROM build AS publish
RUN dotnet publish "SecureBank.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Run as non-privileged application user for container security compliance
USER $APP_UID

# Expose standard ASP.NET Core container ports
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecureBank.dll"]
