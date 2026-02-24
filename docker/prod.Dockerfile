FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY lib/Sanitization/Sanitization.csproj lib/Sanitization/
COPY lib/Observability/Observability.csproj lib/Observability/
COPY lib/CoordinateService/CoordinateService.csproj lib/CoordinateService/
COPY lib/ lib/
COPY config/ config/
RUN dotnet restore lib/CoordinateService/CoordinateService.csproj
RUN dotnet publish lib/CoordinateService/CoordinateService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY config/appsettings.json .
COPY config/appsettings.Production.json .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoordinateService.dll"]
