# Build the API using the .NET 10 SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy SDK settings and project files first for restore caching.
COPY global.json ./

COPY src/MarketApp.Api/MarketApp.Api.csproj src/MarketApp.Api/
COPY src/MarketApp.Application/MarketApp.Application.csproj src/MarketApp.Application/
COPY src/MarketApp.Domain/MarketApp.Domain.csproj src/MarketApp.Domain/
COPY src/MarketApp.Infrastructure/MarketApp.Infrastructure.csproj src/MarketApp.Infrastructure/

RUN dotnet restore src/MarketApp.Api/MarketApp.Api.csproj

# Copy source code and publish.
COPY src/ src/

RUN dotnet publish src/MarketApp.Api/MarketApp.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Run using the smaller ASP.NET Core runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Run as the image's non-root application user.
USER $APP_UID

# Use Render's assigned port, falling back to 8080 locally.
ENTRYPOINT ["sh", "-c", "exec dotnet MarketApp.Api.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]