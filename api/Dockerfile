FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY proto ./proto
COPY api/Fabricore.slnx api/Directory.Build.props api/
COPY api/Domain/Domain.csproj api/Domain/
COPY api/Api/Api.csproj api/Api/
RUN cd web && dotnet restore Api/Api.csproj

COPY api/ ./api/
RUN cd web && dotnet publish Api/Api.csproj -c Release -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "10001" \
    appuser

USER appuser

COPY --from=build /publish ./

ENTRYPOINT [ "dotnet", "Api.dll" ]
