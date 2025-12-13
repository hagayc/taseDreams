FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TaseDreams.Api/TaseDreams.Api.csproj", "TaseDreams.Api/"]
RUN dotnet restore "TaseDreams.Api/TaseDreams.Api.csproj"
COPY . .
WORKDIR "/src/TaseDreams.Api"
RUN dotnet build "TaseDreams.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaseDreams.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaseDreams.Api.dll"]

