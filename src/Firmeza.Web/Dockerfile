# Etapa 1: compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Firmeza.Core/Firmeza.Core.csproj", "src/Firmeza.Core/"]
COPY ["src/Firmeza.Infrastructure/Firmeza.Infrastructure.csproj", "src/Firmeza.Infrastructure/"]
COPY ["src/Firmeza.Web/Firmeza.Web.csproj", "src/Firmeza.Web/"]

RUN dotnet restore "src/Firmeza.Web/Firmeza.Web.csproj"

COPY . .
RUN dotnet publish "src/Firmeza.Web/Firmeza.Web.csproj" -c Release -o /app/publish

# Etapa 2: imagen final solo con el runtime (más liviana)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Firmeza.Web.dll"]
