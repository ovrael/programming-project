#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
#EXPOSE 80
#EXPOSE 443
EXPOSE 20222
EXPOSE 30222

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ProgrammingProjectApplication.csproj", "."]
RUN dotnet restore "./ProgrammingProjectApplication.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProgrammingProjectApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProgrammingProjectApplication.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProgrammingProjectApplication.dll"]