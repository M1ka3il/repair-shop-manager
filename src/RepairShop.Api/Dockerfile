# Multi-stage build: the SDK image compiles, the much smaller ASP.NET runtime
# image ships. Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first, so a code-only change reuses the cached
# restore layer instead of re-downloading every package.
COPY Directory.Build.props NuGet.config ./
COPY src/RepairShop.Domain/RepairShop.Domain.csproj src/RepairShop.Domain/
COPY src/RepairShop.Application/RepairShop.Application.csproj src/RepairShop.Application/
COPY src/RepairShop.Infrastructure/RepairShop.Infrastructure.csproj src/RepairShop.Infrastructure/
COPY src/RepairShop.Api/RepairShop.Api.csproj src/RepairShop.Api/
RUN dotnet restore src/RepairShop.Api/RepairShop.Api.csproj

COPY src/ src/
RUN dotnet publish src/RepairShop.Api/RepairShop.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root by default in the .NET 8+ images, but stated explicitly so it is not
# lost if the base image changes.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RepairShop.Api.dll"]
