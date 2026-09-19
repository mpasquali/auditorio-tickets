# 1. Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para aprovechar el caché de capas de Docker
COPY ["AuditorioTickets.Api/AuditorioTickets.Api.csproj", "AuditorioTickets.Api/"]
COPY ["AuditorioTickets.Shared/AuditorioTickets.Shared.csproj", "AuditorioTickets.Shared/"]

# Restaurar dependencias
RUN dotnet restore "AuditorioTickets.Api/AuditorioTickets.Api.csproj"

# Copiar el resto del código fuente
COPY . .

# Compilar y publicar la API en modo Release
WORKDIR "/src/AuditorioTickets.Api"
RUN dotnet publish "AuditorioTickets.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etapa de ejecución (Runtime ligero)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Puerto que Render asigna y expone por defecto para contenedores
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Punto de entrada de la API
ENTRYPOINT ["dotnet", "AuditorioTickets.Api.dll"]