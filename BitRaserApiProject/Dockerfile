FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 4000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
# Build the project directly (not solution) to avoid NETSDK1194 warning
# Suppress all warnings with -p:NoWarn for cleaner Docker build output
RUN dotnet publish BitRaserApiProject/BitRaserApiProject.csproj -c Release -o /app/publish -p:TreatWarningsAsErrors=false --nologo
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BitRaserApiProject.dll"]