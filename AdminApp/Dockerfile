# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AdminApp/AdminApp.csproj", "AdminApp/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "AdminApp/AdminApp.csproj"
COPY . .
WORKDIR /src/AdminApp
RUN dotnet publish AdminApp.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Make sure uploads path exists (also mount a volume in compose)
RUN mkdir -p /app/wwwroot/uploads
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","AdminApp.dll"]