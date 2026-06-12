FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/api/AuthService.DemoApi/AuthService.DemoApi.csproj", "src/api/AuthService.DemoApi/"]
RUN dotnet restore "src/api/AuthService.DemoApi/AuthService.DemoApi.csproj"
COPY . .
WORKDIR "/src/src/api/AuthService.DemoApi"
RUN dotnet build "AuthService.DemoApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.DemoApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.DemoApi.dll"]
