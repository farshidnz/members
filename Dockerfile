FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /code
COPY SettingsAPI.sln ./
COPY ./SettingsAPI ./SettingsAPI
COPY ./SettingsAPI.Tests ./SettingsAPI.Tests
RUN dotnet restore "SettingsAPI.sln"
RUN dotnet build "SettingsAPI.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SettingsAPI.sln" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SettingsAPI.dll"]