# Build stage uses the full .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Reshebnik.Web.csproj ./
COPY Reshebnik.Handlers/Reshebnik.Handlers.csproj Reshebnik.Handlers/
COPY Reshebnik.Domain/Reshebnik.Domain.csproj Reshebnik.Domain/
COPY Reshebnik.EntityFramework/Reshebnik.EntityFramework.csproj Reshebnik.EntityFramework/
COPY Reshebnik.Clickhouse/Reshebnik.Clickhouse.csproj Reshebnik.Clickhouse/
RUN dotnet restore "Reshebnik.Web.csproj"

COPY . .
RUN dotnet publish "Reshebnik.Web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
COPY appsettings.Production.json ./appsettings.Production.json

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://+:8080"

ENTRYPOINT ["dotnet", "Reshebnik.Web.dll"]
