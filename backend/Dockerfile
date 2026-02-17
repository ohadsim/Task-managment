# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY TaskManagement.Domain/TaskManagement.Domain.csproj TaskManagement.Domain/
COPY TaskManagement.Application/TaskManagement.Application.csproj TaskManagement.Application/
COPY TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj TaskManagement.Infrastructure/
COPY TaskManagement.Api/TaskManagement.Api.csproj TaskManagement.Api/

# Restore each project individually (slnx format)
RUN dotnet restore TaskManagement.Api/TaskManagement.Api.csproj

# Copy all source
COPY . .

# Publish
RUN dotnet publish TaskManagement.Api/TaskManagement.Api.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
