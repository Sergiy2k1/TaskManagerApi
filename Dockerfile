# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project metadata first so NuGet restore can be cached independently
# from ordinary source-code changes.
COPY Directory.Build.props ./
COPY dotnet-tools.json ./
COPY TaskManager.Domain/TaskManager.Domain.csproj TaskManager.Domain/
COPY TaskManager.Application/TaskManager.Application.csproj TaskManager.Application/
COPY TaskManager.Infrastructure/TaskManager.Infrastructure.csproj TaskManager.Infrastructure/
COPY TaskManager.Api/TaskManager.Api.csproj TaskManager.Api/

RUN dotnet restore TaskManager.Api/TaskManager.Api.csproj
RUN dotnet tool restore

COPY . .

RUN dotnet publish TaskManager.Api/TaskManager.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


# One-shot image target used by Docker Compose to apply EF Core migrations
# after PostgreSQL is healthy and before the API starts.
FROM build AS migrations
WORKDIR /src

ENTRYPOINT ["dotnet", "tool", "run", "dotnet-ef", "database", "update", "--project", "TaskManager.Infrastructure/TaskManager.Infrastructure.csproj", "--startup-project", "TaskManager.Api/TaskManager.Api.csproj", "--configuration", "Release", "--no-build"]


# Final runtime image contains only the published application and the
# ASP.NET Core runtime, not the SDK or source tree.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Official .NET images expose APP_UID for the non-root application user.
USER $APP_UID

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
