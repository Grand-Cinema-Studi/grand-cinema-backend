# Build stage: restores, publishes the API and builds the migration bundle
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json dotnet-tools.json ./
COPY src/GrandCinema.Domain/GrandCinema.Domain.csproj src/GrandCinema.Domain/
COPY src/GrandCinema.Application/GrandCinema.Application.csproj src/GrandCinema.Application/
COPY src/GrandCinema.Infrastructure/GrandCinema.Infrastructure.csproj src/GrandCinema.Infrastructure/
COPY src/GrandCinema.Api/GrandCinema.Api.csproj src/GrandCinema.Api/
RUN dotnet tool restore && dotnet restore src/GrandCinema.Api/GrandCinema.Api.csproj

COPY src/ src/
RUN dotnet publish src/GrandCinema.Api/GrandCinema.Api.csproj -c Release -o /app/publish --no-restore

# The design-time tools build the host, which requires a connection string: a placeholder is enough to build the bundle
RUN ConnectionStrings__Default="Host=placeholder" dotnet ef migrations bundle \
    --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api \
    --configuration Release --output /app/publish/efbundle

# Runtime stage: only the ASP.NET Core runtime and the published files
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
LABEL org.opencontainers.image.source=https://github.com/Grand-Cinema-Studi/grand-cinema-backend
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GrandCinema.Api.dll"]
