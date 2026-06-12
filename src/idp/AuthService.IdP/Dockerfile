FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/idp/AuthService.IdP/AuthService.IdP.csproj", "src/idp/AuthService.IdP/"]
COPY ["src/shared/AuthService.Shared/AuthService.Shared.csproj", "src/shared/AuthService.Shared/"]
RUN dotnet restore "src/idp/AuthService.IdP/AuthService.IdP.csproj"
COPY . .
WORKDIR "/src/src/idp/AuthService.IdP"
RUN dotnet build "AuthService.IdP.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.IdP.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.IdP.dll"]
