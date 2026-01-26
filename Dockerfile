# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build configuration files first for better layer caching
COPY Directory.Build.props .
COPY global.json .
COPY .editorconfig .
COPY GitVersion.yml .

# Copy solution and project files
COPY IntervalGenerator.sln .
COPY src/IntervalGenerator.Core/IntervalGenerator.Core.csproj src/IntervalGenerator.Core/
COPY src/IntervalGenerator.Profiles/IntervalGenerator.Profiles.csproj src/IntervalGenerator.Profiles/
COPY src/IntervalGenerator.Output/IntervalGenerator.Output.csproj src/IntervalGenerator.Output/
COPY src/IntervalGenerator.Api/IntervalGenerator.Api.csproj src/IntervalGenerator.Api/

# Restore dependencies
RUN dotnet restore src/IntervalGenerator.Api/IntervalGenerator.Api.csproj

# Copy source code
COPY src/ src/

# Build the application
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.0-docker
RUN dotnet build src/IntervalGenerator.Api/IntervalGenerator.Api.csproj -c $BUILD_CONFIGURATION --no-restore -p:Version=$VERSION

# Publish the application
RUN dotnet publish src/IntervalGenerator.Api/IntervalGenerator.Api.csproj -c $BUILD_CONFIGURATION --no-build -o /app/publish -p:Version=$VERSION

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "IntervalGenerator.Api.dll"]
