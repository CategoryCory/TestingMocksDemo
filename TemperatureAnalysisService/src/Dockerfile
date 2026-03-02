# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Libraries/Temperature.Contracts/Temperature.Contracts.csproj Libraries/Temperature.Contracts/
COPY TemperatureAnalysisService/src/TemperatureAnalysisService.csproj TemperatureAnalysisService/src/
RUN dotnet restore TemperatureAnalysisService/src/TemperatureAnalysisService.csproj

COPY Libraries/Temperature.Contracts/ Libraries/Temperature.Contracts/
COPY TemperatureAnalysisService/src/ TemperatureAnalysisService/src/
WORKDIR /src/TemperatureAnalysisService/src
RUN dotnet publish -c Release -o /app/publish

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TemperatureAnalysisService.dll"]