# Build API + Blazor WASM client and serve both from the API (same origin).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ControleViagens.Api/ControleViagens.Api.csproj ControleViagens.Api/
COPY ControleViagens.Client/ControleViagens.Client.csproj ControleViagens.Client/
RUN dotnet restore ControleViagens.Api/ControleViagens.Api.csproj \
    && dotnet restore ControleViagens.Client/ControleViagens.Client.csproj

COPY . .
RUN dotnet publish ControleViagens.Api/ControleViagens.Api.csproj -c Release -o /out/api --no-restore \
    && dotnet publish ControleViagens.Client/ControleViagens.Client.csproj -c Release -o /out/client --no-restore \
    && rm -rf /out/api/wwwroot \
    && cp -r /out/client/wwwroot /out/api/wwwroot \
    && rm -f /out/api/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/api .
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=10000
EXPOSE 10000
# Render provides the port in $PORT (default 10000).
CMD ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT}; exec dotnet ControleViagens.Api.dll"]
