# Build stage uses the full .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Reshebnik.Web.csproj ./
COPY Reshebnik.Handlers/Reshebnik.Handlers.csproj Reshebnik.Handlers/
COPY Reshebnik.Domain/Reshebnik.Domain.csproj Reshebnik.Domain/
COPY Reshebnik.EntityFramework/Reshebnik.EntityFramework.csproj Reshebnik.EntityFramework/
COPY Reshebnik.Clickhouse/Reshebnik.Clickhouse.csproj Reshebnik.Clickhouse/
COPY Reshebnik.Clickhouse/Migrations Reshebnik.Clickhouse/Migrations/
COPY . .

RUN dotnet restore "Reshebnik.Web.csproj"

RUN dotnet publish "Reshebnik.Web.csproj" -c Release -o /app/publish && ls -la /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 443
COPY --from=build /app/publish .
COPY certificate.pfx .
COPY Reshebnik.Clickhouse/Migrations Reshebnik.Clickhouse/Migrations
COPY appsettings.Production.json ./appsettings.Production.json

RUN export DATETIME_NOW=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="https://+:443"
ENV DATETIME_NOW=$DATETIME_NOW

ENTRYPOINT ["dotnet", "Reshebnik.Web.dll"]
