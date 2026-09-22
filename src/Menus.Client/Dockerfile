# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Menus.Client/Menus.Client.csproj src/Menus.Client/
RUN dotnet restore src/Menus.Client/Menus.Client.csproj

COPY src/Menus.Client/ src/Menus.Client/
RUN dotnet publish src/Menus.Client/Menus.Client.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine AS runtime
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
