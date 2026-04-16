# Dockerfile para Railway - Proyecto en subdirectorio CrmService/
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["CrmService/CrmService.csproj", "CrmService/"]
RUN dotnet restore "CrmService/CrmService.csproj"

# Copiar el resto del código y compilar
COPY CrmService/ CrmService/
WORKDIR /src/CrmService
RUN dotnet build "CrmService.csproj" -c Release -o /app/build
RUN dotnet publish "CrmService.csproj" -c Release -o /app/publish

# Imagen final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer puerto (Railway/Render lo asignan dinámicamente)
EXPOSE 5001

# Comando de inicio
ENTRYPOINT ["dotnet", "CrmService.dll"]
