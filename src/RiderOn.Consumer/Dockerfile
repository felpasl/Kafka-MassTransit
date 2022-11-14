#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/RiderOn.Consumer/RideOn.Consumer.csproj", "src/RiderOn.Consumer/"]
COPY ["src/RideOn.Components/RideOn.Components.csproj", "src/RideOn.Components/"]
COPY ["src/RideOn.Contracts/RideOn.Contracts.csproj", "src/RideOn.Contracts/"]
RUN dotnet restore "src/RiderOn.Consumer/RideOn.Consumer.csproj"
COPY . .
WORKDIR "/src/src/RiderOn.Consumer"
RUN dotnet build "RideOn.Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RideOn.Consumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RideOn.Consumer.dll"]