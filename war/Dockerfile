FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["war/war.csproj", "war/"]
RUN dotnet restore "war/war.csproj"
COPY . .
WORKDIR "/src/war"
RUN dotnet build "war.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "war.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "war.dll"]
