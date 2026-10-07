# Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Note: the build context is this project's own directory. azure.yaml sets
# services.froyo-recommendation.project to this folder (docker.context defaults to "."
# relative to it), so project paths below are relative to this folder.
COPY . .

# Restore packages
RUN dotnet restore froyo-recommendation-api.csproj
RUN dotnet build froyo-recommendation-api.csproj -c Release --no-restore
RUN dotnet publish froyo-recommendation-api.csproj -c Release --no-build -o /app

# Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8088
EXPOSE 8088
ENTRYPOINT ["dotnet", "froyo-recommendation-api.dll"]
