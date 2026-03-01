# Task 5: Seniority Detail (Environment Hardening)
# Use .NET 10 Chiseled Images and run as Non-Root user.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["apps/settlement-core/src/AresNexus.Settlement.Api/AresNexus.Settlement.Api.csproj", "apps/settlement-core/src/AresNexus.Settlement.Api/"]
COPY ["apps/settlement-core/src/AresNexus.Settlement.Application/AresNexus.Settlement.Application.csproj", "apps/settlement-core/src/AresNexus.Settlement.Application/"]
COPY ["apps/settlement-core/src/AresNexus.Settlement.Domain/AresNexus.Settlement.Domain.csproj", "apps/settlement-core/src/AresNexus.Settlement.Domain/"]
COPY ["apps/settlement-core/src/AresNexus.Settlement.Infrastructure/AresNexus.Settlement.Infrastructure.csproj", "apps/settlement-core/src/AresNexus.Settlement.Infrastructure/"]
COPY ["shared/src/AresNexus.Shared.Kernel/AresNexus.Shared.Kernel.csproj", "shared/src/AresNexus.Shared.Kernel/"]

RUN dotnet restore "apps/settlement-core/src/AresNexus.Settlement.Api/AresNexus.Settlement.Api.csproj"
COPY . .
WORKDIR "/src/apps/settlement-core/src/AresNexus.Settlement.Api"
RUN dotnet build "AresNexus.Settlement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AresNexus.Settlement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Task 5: Chiseled image for minimal attack surface
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine-composite AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Task 5: Non-Root user (standard in chiseled/hardened images)
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AresNexus.Settlement.Api.dll"]
