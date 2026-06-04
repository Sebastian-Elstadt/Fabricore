FROM node:24-alpine AS node-build
WORKDIR /app

RUN corepack enable

COPY web/package.json web/pnpm-lock.yaml web/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY web/ ./
RUN pnpm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /app

COPY proto ./proto
COPY api/Fabricore.slnx api/Directory.Build.props api/
COPY api/Domain/Domain.csproj api/Domain/
COPY api/Api/Api.csproj api/Api/
RUN cd api && dotnet restore Api/Api.csproj

COPY api/ ./api/
RUN cd api && dotnet publish Api/Api.csproj -c Release -o /publish /p:UseAppHost=false

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

COPY --from=dotnet-build /publish ./
COPY --from=node-build /app/dist/*/browser/ ./wwwroot/

ENTRYPOINT [ "dotnet", "Api.dll" ]
