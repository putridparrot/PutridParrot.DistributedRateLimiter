# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081



# This stage is used to build the web app project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PutridParrot.DistributedRateLimiter.Sample/PutridParrot.DistributedRateLimiter.Sample.csproj", "PutridParrot.DistributedRateLimiter.Sample/"]
COPY ["PutridParrot.DistributedRateLimiter/PutridParrot.DistributedRateLimiter.csproj", "PutridParrot.DistributedRateLimiter/"]
RUN dotnet restore "./PutridParrot.DistributedRateLimiter.Sample/PutridParrot.DistributedRateLimiter.Sample.csproj"
COPY . .
WORKDIR "/src/PutridParrot.DistributedRateLimiter.Sample"
RUN dotnet build "./PutridParrot.DistributedRateLimiter.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the web app project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PutridParrot.DistributedRateLimiter.Sample.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
# Ensure all published files are copied, not just the DLL
COPY --from=publish /app/publish /app
ENTRYPOINT ["dotnet", "PutridParrot.DistributedRateLimiter.Sample.dll"]