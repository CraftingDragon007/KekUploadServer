FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5102

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["KekUploadServer/KekUploadServer.csproj", "KekUploadServer/"]
RUN dotnet restore "KekUploadServer/KekUploadServer.csproj"
COPY . .
WORKDIR "/src/KekUploadServer"
RUN dotnet build "KekUploadServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KekUploadServer.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt update
RUN apt install -y ffmpeg
RUN rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/config/
WORKDIR /app/config
ENTRYPOINT ["dotnet", "../KekUploadServer.dll"]
