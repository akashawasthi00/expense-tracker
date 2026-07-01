# Build context is the repository root: docker build -f ExpenseTracker.Api/Dockerfile .

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy every project file first and restore, so the restore layer is cached unless a csproj changes.
COPY ExpenseTracker.Domain/ExpenseTracker.Domain.csproj ExpenseTracker.Domain/
COPY ExpenseTracker.Application/ExpenseTracker.Application.csproj ExpenseTracker.Application/
COPY ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj ExpenseTracker.Infrastructure/
COPY ExpenseTracker.Api/ExpenseTracker.Api.csproj ExpenseTracker.Api/
RUN dotnet restore ExpenseTracker.Api/ExpenseTracker.Api.csproj

# Copy the source for the projects the API depends on, then publish.
COPY ExpenseTracker.Domain/ ExpenseTracker.Domain/
COPY ExpenseTracker.Application/ ExpenseTracker.Application/
COPY ExpenseTracker.Infrastructure/ ExpenseTracker.Infrastructure/
COPY ExpenseTracker.Api/ ExpenseTracker.Api/
RUN dotnet publish ExpenseTracker.Api/ExpenseTracker.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Listen on 8080 (non-privileged) so we can run as a non-root user
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the built-in non-root user provided by the .NET image
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExpenseTracker.Api.dll"]
