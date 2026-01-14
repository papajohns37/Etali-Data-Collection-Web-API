# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Etali Data Collection Web API/Etali Data Collection Web API.csproj", "Etali Data Collection Web API/"]
RUN dotnet restore "Etali Data Collection Web API/Etali Data Collection Web API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Etali Data Collection Web API"
RUN dotnet build "Etali Data Collection Web API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Etali Data Collection Web API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install wget for health checks (lightweight alternative to curl)
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy the published application
COPY --from=publish /app/publish .

# Expose port 8080 (Render's default)
EXPOSE 8080

# Set environment variable for ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Etali Data Collection Web API.dll"]
