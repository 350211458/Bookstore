# Spec 06 — ApiGateway (YARP reverse proxy, public entry point).
# Build context is the repository root (docker-compose.yaml sets context: .).
# Only this project and its ServiceDefaults project reference are copied; host
# bin/obj (Windows artifacts) are removed before the Linux restore for a
# deterministic build.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ServiceDefaults/", "ServiceDefaults/"]
COPY ["src/Gateways/ApiGateway/", "Gateways/ApiGateway/"]

RUN rm -rf ServiceDefaults/bin ServiceDefaults/obj \
           Gateways/ApiGateway/bin Gateways/ApiGateway/obj \
    && dotnet restore "Gateways/ApiGateway/ApiGateway.csproj"

WORKDIR "/src/Gateways/ApiGateway"
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
