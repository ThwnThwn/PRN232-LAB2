# ------------------------------------------------------------------
# PRN232.LMS.API Dockerfile
# Build context: the Lab 2 solution root (see docker-compose.yml).
# ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (layer caching)
COPY PRN232.LMS.sln ./
COPY Directory.Build.props ./
COPY PRN232.LMS.API/PRN232.LMS.API.csproj PRN232.LMS.API/
COPY PRN232.LMS.Services/PRN232.LMS.Services.csproj PRN232.LMS.Services/
COPY PRN232.LMS.Repositories/PRN232.LMS.Repositories.csproj PRN232.LMS.Repositories/
RUN dotnet restore PRN232.LMS.API/PRN232.LMS.API.csproj

# Copy remaining sources and publish
COPY . .
WORKDIR /src/PRN232.LMS.API
RUN dotnet publish PRN232.LMS.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PRN232.LMS.API.dll"]
