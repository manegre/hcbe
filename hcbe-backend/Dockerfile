# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the web project file and restore (better cache)
COPY HcbeApi.csproj ./
RUN dotnet restore HcbeApi.csproj

# Copy the rest of the source and build only the web project
COPY . .
RUN dotnet build HcbeApi.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish HcbeApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Keep a local upload directory for development-compatible container runs.
# Production deployments use S3-compatible object storage.
RUN mkdir -p /app/data/uploads /app/wwwroot && \
    ln -s /app/data/uploads /app/wwwroot/uploads

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Railway injects PORT; other container platforms fall back to 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet HcbeApi.dll $@", "--"]

