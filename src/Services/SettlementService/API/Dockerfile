FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/SettlementService/API/AresNexus.Settlement.Api.csproj", "src/Services/SettlementService/API/"]
COPY ["src/Services/SettlementService/Application/AresNexus.Settlement.Application.csproj", "src/Services/SettlementService/Application/"]
COPY ["src/Services/SettlementService/Domain/AresNexus.Settlement.Domain.csproj", "src/Services/SettlementService/Domain/"]
COPY ["src/Services/SettlementService/Infrastructure/AresNexus.Settlement.Infrastructure.csproj", "src/Services/SettlementService/Infrastructure/"]
COPY ["src/BuildingBlocks/Domain/AresNexus.Shared.Kernel.csproj", "src/BuildingBlocks/Domain/"]

RUN dotnet restore "src/Services/SettlementService/API/AresNexus.Settlement.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/SettlementService/API"
RUN dotnet build "AresNexus.Settlement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AresNexus.Settlement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AresNexus.Settlement.Api.dll"]
