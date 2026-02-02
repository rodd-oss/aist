# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/Aist.Shared/Aist.Shared.csproj src/Aist.Shared/
COPY src/Aist.Backend/Aist.Backend.csproj src/Aist.Backend/
RUN dotnet restore src/Aist.Backend/Aist.Backend.csproj

# Copy everything else and build
COPY src/Aist.Shared/ src/Aist.Shared/
COPY src/Aist.Backend/ src/Aist.Backend/
RUN dotnet publish src/Aist.Backend/Aist.Backend.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create directory for SQLite database
RUN mkdir -p /app/data

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 5192

# Set environment
ENV ASPNETCORE_URLS=http://+:5192
ENV ASPNETCORE_ENVIRONMENT=Production

# Create volume for database persistence
VOLUME ["/app/data"]

# Entry point
ENTRYPOINT ["dotnet", "Aist.Backend.dll"]
