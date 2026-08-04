FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VaultLedger.sln ./
COPY src/VaultLedger.Domain/VaultLedger.Domain.csproj src/VaultLedger.Domain/
COPY src/VaultLedger.Application/VaultLedger.Application.csproj src/VaultLedger.Application/
COPY src/VaultLedger.Infrastructure/VaultLedger.Infrastructure.csproj src/VaultLedger.Infrastructure/
COPY src/VaultLedger.API/VaultLedger.API.csproj src/VaultLedger.API/
RUN dotnet restore src/VaultLedger.API/VaultLedger.API.csproj

COPY src/ src/
RUN dotnet publish src/VaultLedger.API/VaultLedger.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app .

ENTRYPOINT ["dotnet", "VaultLedger.API.dll"]
