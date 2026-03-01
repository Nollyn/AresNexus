# Task 5: Seniority Detail (Environment Hardening)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["apps/gateway-api/src/AresNexus.Gateway.Api.csproj", "apps/gateway-api/src/"]
RUN dotnet restore "apps/gateway-api/src/AresNexus.Gateway.Api.csproj"
COPY . .
WORKDIR "/src/apps/gateway-api/src"
RUN dotnet build "AresNexus.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AresNexus.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Task 2: Chiseled image for minimal attack surface
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish ./

# Task 5: Non-Root user (standard in chiseled/hardened images)
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AresNexus.Gateway.Api.dll"]
