# Multi-stage build for both backend containers.
# docker compose selects the entry: target=api or target=worker.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Planorama.sln .
COPY src/Planorama.Core/Planorama.Core.csproj src/Planorama.Core/
COPY src/Planorama.Api/Planorama.Api.csproj src/Planorama.Api/
COPY src/Planorama.Worker/Planorama.Worker.csproj src/Planorama.Worker/
COPY tests/Planorama.Tests/Planorama.Tests.csproj tests/Planorama.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish src/Planorama.Api -c Release -o /out/api --no-restore \
 && dotnet publish src/Planorama.Worker -c Release -o /out/worker --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
# curl is needed by the compose healthcheck; the base image doesn't include it.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/api .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Planorama.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS worker
WORKDIR /app
COPY --from=build /out/worker .
ENTRYPOINT ["dotnet", "Planorama.Worker.dll"]
