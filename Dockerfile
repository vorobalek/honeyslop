FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS base
ENV ASPNETCORE_HTTP_PORTS=
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY . .
RUN dotnet restore "Honeyslop.sln"

FROM restore AS build
RUN dotnet build "Honeyslop.sln" -c Release --no-restore

FROM build AS tested
RUN dotnet test "Honeyslop.sln" -c Release --no-build --filter "TestCategory!=Integration"

FROM tested AS publish
RUN dotnet publish "src/Honeyslop.Migrator/Honeyslop.Migrator.csproj" -c Release -o /app/migrator /p:UseAppHost=false --no-build
RUN dotnet publish "src/Honeyslop.Trap/Honeyslop.Trap.csproj" -c Release -o /app/trap /p:UseAppHost=false --no-build
RUN dotnet publish "src/Honeyslop.Admin/Honeyslop.Admin.csproj" -c Release -o /app/admin /p:UseAppHost=false --no-build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS geoip
WORKDIR /geoip
RUN for month in "$(date -u +%Y-%m)" "$(date -u -d "$(date -u +%Y-%m-01) -1 month" +%Y-%m)"; do \
        curl -fsSL "https://download.db-ip.com/free/dbip-country-lite-$month.mmdb.gz" -o country.mmdb.gz && break; \
    done && \
    gunzip country.mmdb.gz

### migrator
FROM base AS migrator
WORKDIR /app
COPY --from=publish /app/migrator .
ENTRYPOINT ["dotnet", "Honeyslop.Migrator.dll"]

### trap
FROM base AS trap
WORKDIR /app
COPY --from=publish /app/trap .
ENTRYPOINT ["dotnet", "Honeyslop.Trap.dll"]

### admin
FROM base AS admin
WORKDIR /app
COPY --from=publish /app/admin .
COPY --from=geoip /geoip/country.mmdb /app/geoip/country.mmdb
ENTRYPOINT ["dotnet", "Honeyslop.Admin.dll"]
