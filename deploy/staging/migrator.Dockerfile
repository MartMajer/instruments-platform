FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src
COPY . .
RUN dotnet tool restore
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/Platform.Infrastructure/Platform.Infrastructure.csproj", "--startup-project", "src/Platform.Infrastructure/Platform.Infrastructure.csproj"]
