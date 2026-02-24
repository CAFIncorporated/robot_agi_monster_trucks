FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CoordinateService.sln .
COPY lib/Sanitization/Sanitization.csproj lib/Sanitization/
COPY lib/Observability/Observability.csproj lib/Observability/
COPY lib/CoordinateService/CoordinateService.csproj lib/CoordinateService/
COPY clients/CoordinateService.Client/CoordinateService.Client.csproj clients/CoordinateService.Client/
COPY test/CoordinateService.Tests/CoordinateService.Tests.csproj test/CoordinateService.Tests/
COPY clients_test/CoordinateService.ClientTests/CoordinateService.ClientTests.csproj clients_test/CoordinateService.ClientTests/
COPY lib/ lib/
COPY test/ test/
COPY clients/ clients/
COPY clients_test/ clients_test/
COPY config/ config/
COPY openapi.json ./
RUN dotnet restore
RUN dotnet build --no-restore -c Release

ENTRYPOINT ["dotnet", "test", "--no-build", "-c", "Release", "--verbosity", "normal", "--logger", "trx;LogFileName=results.trx"]
