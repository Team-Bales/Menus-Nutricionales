# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MenusNutricionales.slnx .
COPY src/Menus.Domain/Menus.Domain.csproj src/Menus.Domain/
COPY src/Menus.Application/Menus.Application.csproj src/Menus.Application/
COPY src/Menus.Infrastructure/Menus.Infrastructure.csproj src/Menus.Infrastructure/
COPY src/Menus.Api/Menus.Api.csproj src/Menus.Api/
RUN dotnet restore src/Menus.Api/Menus.Api.csproj

COPY src/Menus.Domain/ src/Menus.Domain/
COPY src/Menus.Application/ src/Menus.Application/
COPY src/Menus.Infrastructure/ src/Menus.Infrastructure/
COPY src/Menus.Api/ src/Menus.Api/

RUN dotnet publish src/Menus.Api/Menus.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Menus.Api.dll"]
