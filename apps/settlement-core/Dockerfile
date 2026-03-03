# Task 5: Seniority Detail (Environment Hardening)
# Use .NET 10 Chiseled Images and run as Non-Root user.

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
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

# Task 3: Chiseled image for minimal attack surface (Senior Architect Requirement)
# Chiseled images do not contain a shell or package manager, reducing CVE footprint.
# Note: Reverting to standard preview tag until Chiseled variants for .NET 10 are officially published in the Microsoft Container Registry (MCR).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Harden the image by patching known CVEs (e.g., CVE-2025-68973 in gpgv)
# Since we're not yet using Chiseled images, we still have access to apt.
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    gpgv \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID

COPY --from=publish /app/publish ./

# Task 5: Non-Root user (standard in chiseled/hardened images)
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AresNexus.Settlement.Api.dll"]
