# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore first (better layer caching)
COPY ExpenseTracker.Api/ExpenseTracker.Api.csproj ExpenseTracker.Api/
RUN dotnet restore ExpenseTracker.Api/ExpenseTracker.Api.csproj

# Copy the rest of the source and publish
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
