# Build context is the `backend/` directory (see docker-compose.yml), so every COPY
# path below is relative to `backend/`, not to this file's own folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using just the .csproj files, so this layer caches across code changes.
COPY src/GermanyDashboard.Domain/GermanyDashboard.Domain.csproj src/GermanyDashboard.Domain/
COPY src/GermanyDashboard.Application/GermanyDashboard.Application.csproj src/GermanyDashboard.Application/
COPY src/GermanyDashboard.Infrastructure/GermanyDashboard.Infrastructure.csproj src/GermanyDashboard.Infrastructure/
COPY src/GermanyDashboard.Api/GermanyDashboard.Api.csproj src/GermanyDashboard.Api/
RUN dotnet restore src/GermanyDashboard.Api/GermanyDashboard.Api.csproj

COPY src/ src/
RUN dotnet publish src/GermanyDashboard.Api/GermanyDashboard.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used only by docker-compose's healthcheck for this service.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .
COPY src/GermanyDashboard.Api/entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["./entrypoint.sh"]
