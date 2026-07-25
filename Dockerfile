FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CASAPahampang.sln", "./"]
COPY ["nuget.config", "./"]
COPY ["CASAPahampang/CASAPahampang.csproj", "CASAPahampang/"]
COPY ["CASAPahampang.Client/CASAPahampang.Client.csproj", "CASAPahampang.Client/"]

ARG GITHUB_PAT
ENV GITHUB_PAT=${GITHUB_PAT}
RUN dotnet restore "CASAPahampang.sln"

COPY . .

WORKDIR "/src/CASAPahampang"
RUN dotnet publish "CASAPahampang.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ENTRYPOINT ["dotnet", "CASAPahampang.dll"]