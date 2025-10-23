# Build stage uses the full .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Tabligo.Web.csproj ./
COPY Tabligo.Handlers/Tabligo.Handlers.csproj Tabligo.Handlers/
COPY Tabligo.Domain/Tabligo.Domain.csproj Tabligo.Domain/
COPY Tabligo.EntityFramework/Tabligo.EntityFramework.csproj Tabligo.EntityFramework/
COPY Tabligo.Clickhouse/Tabligo.Clickhouse.csproj Tabligo.Clickhouse/
COPY Tabligo.Clickhouse/Migrations Tabligo.Clickhouse/Migrations/
COPY . .

RUN dotnet restore "Tabligo.Web.csproj"

RUN dotnet publish "Tabligo.Web.csproj" -c Release -o /app/publish && ls -la /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 443

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
# Install certificates for SberGPT gRPC connections
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

# Copy Russian Trusted Root CA certificate
COPY certificates/russian_trusted_root_ca_pem.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates

# Set environment variable for gRPC SSL roots
ENV GRPC_DEFAULT_SSL_ROOTS_FILE_PATH="/usr/local/share/ca-certificates/russian_trusted_root_ca_pem.crt"

COPY --from=build /app/publish .
COPY Tabligo.Clickhouse/Migrations Tabligo.Clickhouse/Migrations
COPY appsettings.Production.json ./appsettings.Production.json

# Copy certificate if it exists (optional for testing)
COPY certificate.pfx* ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://+:5000;https://+:443"
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

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health/live || exit 1

ENTRYPOINT ["dotnet", "Tabligo.Web.dll"]
