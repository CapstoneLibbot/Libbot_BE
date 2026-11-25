FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LiBot.sln ./
COPY src/LiBot.Api/LiBot.Api.csproj src/LiBot.Api/
COPY src/LiBot.Application/LiBot.Application.csproj src/LiBot.Application/
COPY src/LiBot.Contracts/LiBot.Contracts.csproj src/LiBot.Contracts/
COPY src/LiBot.Domain/LiBot.Domain.csproj src/LiBot.Domain/
COPY src/LiBot.Infrastructure/LiBot.Infrastructure.csproj src/LiBot.Infrastructure/
RUN dotnet restore src/LiBot.Api/LiBot.Api.csproj

COPY src/ src/
COPY docs/database/ docs/database/
RUN dotnet publish src/LiBot.Api/LiBot.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LiBot.Api.dll"]
