# Use the official .NET 8 SDK image as the build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY ./src/*.csproj ./
RUN dotnet restore

# Copy the source code into the container
COPY ./src/ ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 8 ASP.NET Core image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy the published files from the build image
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "LegalEntities.dll"]
