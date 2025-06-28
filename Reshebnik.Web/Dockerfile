FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Reshebnik.Web.csproj", "./"]
COPY ["Reshebnik.Handlers/Reshebnik.Handlers.csproj", "Reshebnik.Handlers/"]
COPY ["Reshebnik.Domain/Reshebnik.Domain.csproj", "Reshebnik.Domain/"]
COPY ["Reshebnik.EntityFramework/Reshebnik.EntityFramework.csproj", "Reshebnik.EntityFramework/"]
RUN dotnet restore "Reshebnik.Web.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Reshebnik.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Reshebnik.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=""
ENV DOTNET_Kestrel__Endpoints__Http__Url="http://0.0.0.0:8080"

ENTRYPOINT ["dotnet", "Reshebnik.Web.dll"]
