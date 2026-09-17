# Etapa 1: Compilar la aplicación Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "AuditorioTickets.Client/AuditorioTickets.Client.csproj" -c Release -o /app/publish

# Etapa 2: Servir los archivos estáticos con Nginx
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html