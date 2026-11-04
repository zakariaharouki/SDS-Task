# Build on Render with Root Directory "backend" and Dockerfile Path "./Dockerfile".
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore before copying the code, so code changes don't repeat the package download.
COPY SchemeTracker.Api/SchemeTracker.Api.csproj SchemeTracker.Api/
RUN dotnet restore SchemeTracker.Api/SchemeTracker.Api.csproj

COPY SchemeTracker.Api/ SchemeTracker.Api/
RUN dotnet publish SchemeTracker.Api/SchemeTracker.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# The non-root user that the .NET 8 images provide.
USER $APP_UID

EXPOSE 8080

# Render sets $PORT. It falls back to 8080 elsewhere. Settings come from environment variables
# at runtime, so no credentials are ever part of the image.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_HTTP_PORTS=${PORT:-8080} && exec dotnet SchemeTracker.Api.dll"]
