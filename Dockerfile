# ==========================================================
# Build stage
# ==========================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project file first to make restore faster
COPY ["MarketLine.csproj", "./"]

RUN dotnet restore "./MarketLine.csproj"

# Copy the rest of the project
COPY . .

# Publish the application
RUN dotnet publish "./MarketLine.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ==========================================================
# Runtime stage
# ==========================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Render normally provides port 10000 by default.
EXPOSE 10000

COPY --from=build /app/publish .

# This directory will later be mounted to a Render Persistent Disk.
# Your existing upload code already uses wwwroot/uploads.
RUN mkdir -p /app/wwwroot/uploads

# Render provides the PORT environment variable.
# If unavailable, the application uses port 10000.
ENTRYPOINT ["sh", "-c", "dotnet MarketLine.dll --urls http://0.0.0.0:${PORT:-10000}"]