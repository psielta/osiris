FROM node:22-bookworm-slim AS web-assets
WORKDIR /src/src/Osiris.Web

COPY src/Osiris.Web/package*.json ./
RUN npm ci

COPY src/Osiris.Web/Styles ./Styles
COPY src/Osiris.Web/scripts ./scripts
COPY src/Osiris.Web/Views ./Views
COPY src/Osiris.Web/Controllers ./Controllers
COPY src/Osiris.Web/Models ./Models
COPY src/Osiris.Web/Helpers ./Helpers
RUN npm run copy:assets && npm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Osiris.Domain/Osiris.Domain.csproj src/Osiris.Domain/
COPY src/Osiris.Application/Osiris.Application.csproj src/Osiris.Application/
COPY src/Osiris.Infrastructure/Osiris.Infrastructure.csproj src/Osiris.Infrastructure/
COPY src/Osiris.Web/Osiris.Web.csproj src/Osiris.Web/
RUN dotnet restore src/Osiris.Web/Osiris.Web.csproj

COPY src ./src
COPY --from=web-assets /src/src/Osiris.Web/wwwroot/css/app.css src/Osiris.Web/wwwroot/css/app.css
COPY --from=web-assets /src/src/Osiris.Web/wwwroot/lib/alpinejs/cdn.min.js src/Osiris.Web/wwwroot/lib/alpinejs/cdn.min.js
COPY --from=web-assets /src/src/Osiris.Web/wwwroot/lib/htmx/htmx.min.js src/Osiris.Web/wwwroot/lib/htmx/htmx.min.js
RUN dotnet publish src/Osiris.Web/Osiris.Web.csproj -c Release -o /app/publish --no-restore /p:SkipWebAssetBuild=true

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrations
WORKDIR /src
ENV PATH="/root/.dotnet/tools:${PATH}"

RUN dotnet tool install --global dotnet-ef --version 10.0.8
COPY Directory.Build.props ./
COPY src ./src
RUN dotnet restore src/Osiris.Web/Osiris.Web.csproj
RUN dotnet build src/Osiris.Web/Osiris.Web.csproj -c Release --no-restore /p:SkipWebAssetBuild=true
ENTRYPOINT ["dotnet-ef", "database", "update", "--project", "src/Osiris.Infrastructure", "--startup-project", "src/Osiris.Web", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# QuestPDF renders PDFs through SkiaSharp, which needs fontconfig present on Linux.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://0.0.0.0:13453
EXPOSE 13453

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Osiris.Web.dll"]
