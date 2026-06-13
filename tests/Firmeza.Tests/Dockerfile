FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Firmeza.slnx", "./"]
COPY ["src/Firmeza.Core/Firmeza.Core.csproj", "src/Firmeza.Core/"]
COPY ["src/Firmeza.Infrastructure/Firmeza.Infrastructure.csproj", "src/Firmeza.Infrastructure/"]
COPY ["src/Firmeza.Web/Firmeza.Web.csproj", "src/Firmeza.Web/"]
COPY ["src/Firmeza.Api/Firmeza.Api.csproj", "src/Firmeza.Api/"]
COPY ["tests/Firmeza.Tests/Firmeza.Tests.csproj", "tests/Firmeza.Tests/"]

RUN dotnet restore

COPY . .
ENTRYPOINT ["dotnet", "test", "tests/Firmeza.Tests/Firmeza.Tests.csproj", "--logger:console;verbosity=normal"]
