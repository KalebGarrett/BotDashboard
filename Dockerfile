# Bot Harbor — self-hosted image for the Cortex NAS (linux/amd64).
#
# Build from the repository root so both projects are in context:
#   docker build -t kalebg08/botharbor:latest .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, against the project files only, so a code-only change reuses the package layer.
COPY BotDashboard.sln ./
COPY BotDashboard.App/BotDashboard.App.csproj BotDashboard.App/
COPY BotDashboard.Models/BotDashboard.Models.csproj BotDashboard.Models/
RUN dotnet restore BotDashboard.App/BotDashboard.App.csproj

COPY BotDashboard.App/ BotDashboard.App/
COPY BotDashboard.Models/ BotDashboard.Models/

# Deliberately restoring again here rather than passing --no-restore. A restore run before
# the source is present does not resolve the framework's static web assets, and --no-restore
# then never revisits that: the publish succeeds, but wwwroot/_framework is missing, so
# /_framework/blazor.web.js 404s, Blazor never boots, and every interactive component
# silently stops working. .NET 8 tolerated this; .NET 10 does not. The repeat restore is
# cheap because the packages are already in the layer above.
RUN dotnet publish BotDashboard.App/BotDashboard.App.csproj \
        --configuration Release \
        --output /app

# Fail the build rather than ship an image whose Blazor script is missing. The symptom is a
# page that renders correctly and then does nothing at all, which is painful to diagnose
# from the browser -- Cloudflare even reports the 404 as a 503.
RUN test -f /app/wwwroot/_framework/blazor.web.js \
    || (echo 'FATAL: wwwroot/_framework/blazor.web.js missing from publish output' && exit 1)


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only here so the compose healthcheck can probe /healthz.
RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

# Kestrel serves plain HTTP on 8080; the Tailscale sidecar terminates TLS in front of it.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Data Protection keys live on a mounted volume so auth cookies survive a redeploy.
RUN mkdir -p /var/botharbor/keys && chown -R app:app /var/botharbor

COPY --from=build --chown=app:app /app ./

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "BotDashboard.App.dll"]
