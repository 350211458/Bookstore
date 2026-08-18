# Spec 06 — Identity.Api (OpenIddict OIDC server).
# Build context is the repository root (docker-compose.yaml sets context: .).
# Only this project and its ServiceDefaults project reference are copied; host
# bin/obj (Windows artifacts) are removed before the Linux restore for a
# deterministic build.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ServiceDefaults/", "ServiceDefaults/"]
COPY ["src/Services/Identity.Api/", "Services/Identity.Api/"]

RUN rm -rf ServiceDefaults/bin ServiceDefaults/obj \
           Services/Identity.Api/bin Services/Identity.Api/obj \
    && dotnet restore "Services/Identity.Api/Identity.Api.csproj"

WORKDIR "/src/Services/Identity.Api"
RUN dotnet publish "Identity.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
