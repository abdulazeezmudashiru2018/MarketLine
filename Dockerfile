# =========================================
# STAGE 1: Build
# =========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first and restore (better Docker layer caching)
COPY ["MarketLine.csproj", "./"]
RUN dotnet restore "MarketLine.csproj"

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish "MarketLine.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================
# STAGE 2: Runtime
# =========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Render sets the PORT env variable; default to 5000 locally
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}

# Start the app
ENTRYPOINT ["dotnet", "MarketLine.dll"]