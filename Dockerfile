# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos
COPY ["AuditorioTickets.Api/AuditorioTickets.Api.csproj", "AuditorioTickets.Api/"]
COPY ["AuditorioTickets.Shared/AuditorioTickets.Shared.csproj", "AuditorioTickets.Shared/"]
COPY ["AuditorioTickets.Client/AuditorioTickets.Client.csproj", "AuditorioTickets.Client/"]

# Restaurar dependencias
RUN dotnet restore "AuditorioTickets.Api/AuditorioTickets.Api.csproj"

# Copiar todo el código fuente
COPY . .
WORKDIR "/src/AuditorioTickets.Api"
RUN dotnet build "AuditorioTickets.Api.csproj" -c Release -o /app/build

# Etapa de publicación
FROM build AS publish
RUN dotnet publish "AuditorioTickets.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final (imagen para producción)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Puerto que expone Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuditorioTickets.Api.dll"]