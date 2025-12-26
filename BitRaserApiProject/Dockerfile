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

# ✅ Create keys directory for DataProtection
RUN mkdir -p /app/keys && chmod 700 /app/keys

# ✅ Environment variables for Docker
ENV DISABLE_HTTPS_REDIRECT=true
ENV DATAPROTECTION_KEYS_PATH=/app/keys
ENV ASPNETCORE_URLS=http://+:4000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=4000

ENTRYPOINT ["dotnet", "BitRaserApiProject.dll"]