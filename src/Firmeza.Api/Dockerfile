# Etapa 1: compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Firmeza.Core/Firmeza.Core.csproj", "src/Firmeza.Core/"]
COPY ["src/Firmeza.Infrastructure/Firmeza.Infrastructure.csproj", "src/Firmeza.Infrastructure/"]
COPY ["src/Firmeza.Api/Firmeza.Api.csproj", "src/Firmeza.Api/"]

RUN dotnet restore "src/Firmeza.Api/Firmeza.Api.csproj"

COPY . .
RUN dotnet publish "src/Firmeza.Api/Firmeza.Api.csproj" -c Release -o /app/publish

# Etapa 2: imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Firmeza.Api.dll"]
