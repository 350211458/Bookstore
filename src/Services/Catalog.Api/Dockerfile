# Spec 06 — Catalog.Api (REST book management + gRPC DeductStock).
# Build context is the repository root (docker-compose.yaml sets context: .).
# Only this project and its ServiceDefaults project reference are copied; host
# bin/obj (Windows artifacts) are removed before the Linux restore for a
# deterministic build.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ServiceDefaults/", "ServiceDefaults/"]
COPY ["src/Services/Catalog.Api/", "Services/Catalog.Api/"]

RUN rm -rf ServiceDefaults/bin ServiceDefaults/obj \
           Services/Catalog.Api/bin Services/Catalog.Api/obj \
    && dotnet restore "Services/Catalog.Api/Catalog.Api.csproj"

WORKDIR "/src/Services/Catalog.Api"
RUN dotnet publish "Catalog.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
