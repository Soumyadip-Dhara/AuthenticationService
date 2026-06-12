FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/bff/AuthService.DemoBff/AuthService.DemoBff.csproj", "src/bff/AuthService.DemoBff/"]
COPY ["src/shared/AuthService.Shared/AuthService.Shared.csproj", "src/shared/AuthService.Shared/"]
RUN dotnet restore "src/bff/AuthService.DemoBff/AuthService.DemoBff.csproj"
COPY . .
WORKDIR "/src/src/bff/AuthService.DemoBff"
RUN dotnet build "AuthService.DemoBff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.DemoBff.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.DemoBff.dll"]
