# Task 5: Seniority Detail (Environment Hardening)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["apps/gateway-api/src/AresNexus.Gateway.Api.csproj", "apps/gateway-api/src/"]
RUN dotnet restore "apps/gateway-api/src/AresNexus.Gateway.Api.csproj"
COPY . .
WORKDIR "/src/apps/gateway-api/src"
RUN dotnet build "AresNexus.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AresNexus.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine-composite AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AresNexus.Gateway.Api.dll"]
