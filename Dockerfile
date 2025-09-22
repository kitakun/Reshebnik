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

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="https://+:443"
ARG DATETIME_NOW
ENV DATETIME_NOW=${DATETIME_NOW}

# Environment variables for configuration
ARG CONNECTION_STRING
ENV ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
ARG JWT_SECRET_KEY
ENV JwtSettings__SecretKey=${JWT_SECRET_KEY}
ARG CLICKHOUSE_PASSWORD
ENV Clickhouse__password=${CLICKHOUSE_PASSWORD}
ARG EMAIL_PASSWORD
ENV Email__password=${EMAIL_PASSWORD}
ENV Email__onetimepass=${EMAIL_PASSWORD}
ARG CERTIFICATE_PASSWORD
ENV CERTIFICATE_PASSWORD=${CERTIFICATE_PASSWORD}

ENTRYPOINT ["dotnet", "Reshebnik.Web.dll"]
