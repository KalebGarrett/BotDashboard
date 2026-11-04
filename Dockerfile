# Bot Harbor — self-hosted image for the Cortex NAS (linux/amd64).
#
# Build from the repository root so both projects are in context:
#   docker build -t kalebg08/botharbor:latest .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, against the project files only, so a code-only change reuses the package layer.
COPY BotDashboard.sln ./
COPY BotDashboard.App/BotDashboard.App.csproj BotDashboard.App/
COPY BotDashboard.Models/BotDashboard.Models.csproj BotDashboard.Models/
RUN dotnet restore BotDashboard.App/BotDashboard.App.csproj

COPY BotDashboard.App/ BotDashboard.App/
COPY BotDashboard.Models/ BotDashboard.Models/

RUN dotnet publish BotDashboard.App/BotDashboard.App.csproj \
        --configuration Release \
        --no-restore \
        --output /app


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
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
