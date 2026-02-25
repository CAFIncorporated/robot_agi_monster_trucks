FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY lib/Sanitization/Sanitization.csproj lib/Sanitization/
COPY lib/Observability/Observability.csproj lib/Observability/
COPY lib/CoordinateService/CoordinateService.csproj lib/CoordinateService/
COPY lib/ lib/
COPY config/ config/
RUN dotnet restore lib/CoordinateService/CoordinateService.csproj && \
    dotnet publish lib/CoordinateService/CoordinateService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER root
RUN groupadd --git 10001 svcuser && \
    useradd --uid 10001 --gid svcuser --shell /bin/false --create-home svcuser && \
    chown -R svcuser:svcuser /app

COPY --from=build --chown=svcuser:svcuser /app/publish .
COPY --chown=svcuser:svcuser config/appsettings.json .
COPY --chown=svcuser:svcuser config/appsettings.Production.json .

USER svcuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoordinateService.dll"]
