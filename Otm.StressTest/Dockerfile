FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Otm.StressTest/Otm.StressTest.csproj", "Otm.StressTest/"]
RUN dotnet restore "Otm.StressTest/Otm.StressTest.csproj"
COPY . .
WORKDIR "/src/Otm.StressTest"
RUN dotnet build "Otm.StressTest.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Otm.StressTest.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Otm.StressTest.dll"]
