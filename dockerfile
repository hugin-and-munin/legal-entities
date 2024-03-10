FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ./src/*.csproj ./
RUN dotnet restore

COPY ./src/ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "LegalEntities.dll"]
