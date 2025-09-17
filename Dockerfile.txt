# Use the official .NET 8 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["BitRaserApiProject/*.csproj", "./BitRaserApiProject/"]
RUN dotnet restore "./BitRaserApiProject/BitRaserApiProject.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/BitRaserApiProject"
RUN dotnet publish "BitRaserApiProject.csproj" -c Release -o /app/publish

# Use the official .NET 8 ASP.NET runtime image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variable for ASP.NET Core to listen on port 8080 (Render default)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Expose port 8080
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "BitRaserApiProject.dll"]