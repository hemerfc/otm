#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Otm.Server.csproj", "Server/"]
#COPY ["Shared/Otm.Shared.csproj", "Shared/"]
#COPY ["Client/Otm.Client.csproj", "Client/"]
#COPY ["Client.Api/Otm.Client.Api.csproj", "Client.Api/"]
RUN dotnet restore "Server/Otm.Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Otm.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Otm.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5001
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Otm.Server.dll"]