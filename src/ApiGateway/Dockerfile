FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ApiGateway/AresNexus.Gateway.Api.csproj", "src/ApiGateway/"]
RUN dotnet restore "src/ApiGateway/AresNexus.Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/ApiGateway"
RUN dotnet build "AresNexus.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AresNexus.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AresNexus.Gateway.Api.dll"]
