FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Osiris.Domain/Osiris.Domain.csproj src/Osiris.Domain/
COPY src/Osiris.Application/Osiris.Application.csproj src/Osiris.Application/
COPY src/Osiris.Infrastructure/Osiris.Infrastructure.csproj src/Osiris.Infrastructure/
COPY src/Osiris.Api/Osiris.Api.csproj src/Osiris.Api/
RUN dotnet restore src/Osiris.Api/Osiris.Api.csproj

COPY src ./src
RUN dotnet publish src/Osiris.Api/Osiris.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# QuestPDF (pulled in transitively by Infrastructure) renders through SkiaSharp, which needs
# fontconfig present on Linux. The API never renders PDFs, but keep parity with the Web image.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://0.0.0.0:13455
EXPOSE 13455

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Osiris.Api.dll"]
