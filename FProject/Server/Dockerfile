#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["FProject/Server/FProject.Server.csproj", "FProject/Server/"]
COPY ["FProject/Shared/FProject.Shared.csproj", "FProject/Shared/"]
COPY ["FProject/Client/FProject.Client.csproj", "FProject/Client/"]
RUN dotnet restore "FProject/Server/FProject.Server.csproj"
COPY . .
WORKDIR "/src/FProject/Server"
RUN dotnet build "FProject.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FProject.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FProject.Server.dll"]
