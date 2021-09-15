#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM node:latest AS node_base

RUN echo "NODE Version:" && node --version
RUN echo "NPM Version:" && npm --version

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
COPY --from=node_base . .
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install NodeJs
#RUN apt-get update && \
    #apt-get install -y wget && \
    #apt-get install -y gnupg2 && \
    #wget -qO- https://deb.nodesource.com/setup_14.x | bash - && \
    #apt-get install -y build-essential nodejs
# End Install

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
COPY --from=node_base . .
WORKDIR /src
COPY ["Server/Otm.Server.csproj", "Server/"]
COPY ["Shared/Otm.Shared.csproj", "Shared/"]
COPY ["Client/Otm.Client.csproj", "Client/"]
COPY ["Client.Api/Otm.Client.Api.csproj", "Client.Api/"]
RUN dotnet restore "Server/Otm.Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Otm.Server.csproj" -c Release -o /app/build

FROM build AS publish
COPY --from=node_base . .
RUN dotnet publish "Otm.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Otm.Server.dll"]