# ---- Build stage ----
# Uses the .NET 10 SDK (preview) to match the net10.0 target framework.
# The Tests project is intentionally excluded from the publish step to keep
# the final image lean — xUnit + WebApplicationFactory deps are ~80 MB.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files first so Docker can cache the restore layer.
# Only the four runtime projects are copied here; Tests is excluded.
COPY TaskManagement.Domain/TaskManagement.Domain.csproj           TaskManagement.Domain/
COPY TaskManagement.Application/TaskManagement.Application.csproj TaskManagement.Application/
COPY TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj TaskManagement.Infrastructure/
COPY TaskManagement.Api/TaskManagement.Api.csproj                 TaskManagement.Api/

# Restore only the API entry-point (pulls in all referenced projects transitively).
RUN dotnet restore TaskManagement.Api/TaskManagement.Api.csproj

# Copy all source code (bin/ and obj/ are excluded by .dockerignore).
COPY TaskManagement.Domain/       TaskManagement.Domain/
COPY TaskManagement.Application/  TaskManagement.Application/
COPY TaskManagement.Infrastructure/ TaskManagement.Infrastructure/
COPY TaskManagement.Api/          TaskManagement.Api/

# Publish a self-contained, Release-mode build.
RUN dotnet publish TaskManagement.Api/TaskManagement.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ---- Runtime stage ----
# Use the smaller ASP.NET runtime image (not the full SDK).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Set production environment.
ENV ASPNETCORE_ENVIRONMENT=Production

# Railway injects a PORT environment variable. Program.cs reads it and calls
# app.Urls.Add("http://0.0.0.0:{PORT}"). We EXPOSE 8080 as a documentation
# hint only — the actual port binding is dynamic at runtime.
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
