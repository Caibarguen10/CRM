# 🔧 Guía de Solución de Problemas - CRM Service

Este documento contiene soluciones a problemas comunes que pueden surgir al configurar y ejecutar el proyecto CRM Service.

---

## 📋 Tabla de Contenidos

1. [Problema de Compilación: Microsoft.OpenApi](#1-problema-de-compilación-microsoftopenapi)
2. [Problemas con Base de Datos](#2-problemas-con-base-de-datos)
3. [Errores de Autenticación JWT](#3-errores-de-autenticación-jwt)
4. [Problemas con Paquetes NuGet](#4-problemas-con-paquetes-nuget)
5. [Problemas al Ejecutar Migraciones](#5-problemas-al-ejecutar-migraciones)
6. [Errores de CORS](#6-errores-de-cors)
7. [Verificación de Instalación](#7-verificación-de-instalación)

---

## 1. Problema de Compilación: Microsoft.OpenApi

### 🔴 Error

```
C:\...\CrmService.csproj : error CS0234: 
El tipo o el nombre del espacio de nombres 'Models' no existe 
en el espacio de nombres 'Microsoft.OpenApi' 
(¿falta alguna referencia de ensamblado?)
```

O también:

```
error NU1605: Advertencia como error: Degradación del paquete detectada: 
Microsoft.OpenApi de 2.4.1 a 2.0.0
```

### ✅ Solución

El problema ocurre porque hay un conflicto de versiones entre los paquetes de Swagger/OpenAPI.

**Paso 1**: Verificar el archivo `CrmService.csproj`

Debe contener esta referencia:

```xml
<PackageReference Include="Microsoft.OpenApi" Version="2.4.1" />
```

**Paso 2**: Si no existe, agrégala manualmente

Abre `CrmService/CrmService.csproj` y asegúrate que el `<ItemGroup>` tenga:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.OpenApi" Version="1.6.14" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
  </ItemGroup>
</Project>
```

**Paso 3**: Limpiar y recompilar

```bash
dotnet clean
dotnet build
```

**Resultado esperado**:
```
Compilación correcta.
    0 Errores
```

### 📝 Explicación Técnica

- `Swashbuckle.AspNetCore 10.1.7` requiere `Microsoft.OpenApi >= 2.4.1`
- `Microsoft.AspNetCore.OpenApi 10.0.5` requiere `Microsoft.OpenApi >= 2.0.0`
- Sin la referencia explícita, NuGet puede resolver una versión incompatible
- Agregando `Microsoft.OpenApi 2.4.1` explícitamente, forzamos la versión correcta

---

## 2. Problemas con Base de Datos

### 🔴 Error: "Table 'Users' already exists"

**Causa**: La base de datos ya existe pero intentas crearla de nuevo.

**Solución**:

```bash
# Opción 1: Eliminar la base de datos existente
cd CrmService
rm crm.db crm.db-shm crm.db-wal

# Opción 2: Verificar si ya existe y omitir creación
dotnet run
```

### 🔴 Error: "Database locked"

**Causa**: Otro proceso tiene la base de datos SQLite abierta.

**Solución**:

```bash
# Windows
taskkill /F /IM dotnet.exe

# Linux/Mac
pkill dotnet

# Luego ejecutar de nuevo
dotnet run
```

### 🔴 Error: "No se puede escribir en crm.db"

**Causa**: Permisos insuficientes en el directorio.

**Solución**:

```bash
# Windows - Ejecutar CMD/PowerShell como Administrador
# O dar permisos de escritura a la carpeta CrmService

# Linux/Mac
chmod 755 CrmService
cd CrmService
dotnet run
```

---

## 3. Errores de Autenticación JWT

### 🔴 Error: "401 Unauthorized"

**Causa 1**: No se envió el token en el header

**Solución**:
```bash
# Correcto
curl -H "Authorization: Bearer TU_TOKEN_AQUI" https://localhost:5001/api/clients

# Incorrecto (falta el header)
curl https://localhost:5001/api/clients
```

**Causa 2**: Token expirado (default: 60 minutos)

**Solución**: Hacer login de nuevo para obtener un token fresco.

### 🔴 Error: "403 Forbidden"

**Causa**: El usuario no tiene el rol necesario para esa operación.

**Ejemplo**:
```
Usuario Asesor intenta: DELETE /api/clients/1
Respuesta: 403 Forbidden
Razón: Solo Admin puede eliminar
```

**Solución**: Verificar la matriz de permisos y usar el usuario correcto.

| Acción | Admin | Asesor | Auditor |
|--------|-------|--------|---------|
| Ver clientes | ✅ | ✅ | ✅ |
| Crear clientes | ✅ | ✅ | ❌ |
| Editar clientes | ✅ | ✅ | ❌ |
| Eliminar clientes | ✅ | ❌ | ❌ |

### 🔴 Error: "The token is invalid"

**Causa**: Token mal formado o clave secreta incorrecta.

**Solución**: Verificar que en `appsettings.json` la clave no haya cambiado:

```json
{
  "Jwt": {
    "SecretKey": "MySecretKeyForCrmServiceMicroserviceJwtTokenGeneration2024!",
    "Issuer": "CrmServiceAPI",
    "Audience": "CrmServiceClient",
    "ExpirationMinutes": 60
  }
}
```

⚠️ **IMPORTANTE**: Si cambias `SecretKey`, todos los tokens anteriores se invalidan.

---

## 4. Problemas con Paquetes NuGet

### 🔴 Error: "Package not found"

**Solución**:

```bash
# Limpiar caché de NuGet
dotnet nuget locals all --clear

# Restaurar paquetes
dotnet restore

# Recompilar
dotnet build
```

### 🔴 Error: "Vulnerabilidad detectada en AutoMapper 12.0.1"

**Warning**:
```
NU1903: El paquete "AutoMapper" 12.0.1 tiene una vulnerabilidad 
de gravedad alta conocida
```

**Solución (cuando esté disponible)**:

```bash
# Actualizar a versión sin vulnerabilidad
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 13.0.0
```

**Nota**: Por ahora es solo un warning y no impide la compilación. Actualizar cuando salga versión parcheada.

### 🔴 Error: "Timeout al restaurar paquetes"

**Causa**: Conexión lenta o servidor NuGet caído.

**Solución**:

```bash
# Aumentar timeout
dotnet restore --source https://api.nuget.org/v3/index.json --verbosity detailed

# O usar mirror local si estás en empresa
dotnet restore --source http://tu-mirror-nuget-interno
```

---

## 5. Problemas al Ejecutar Migraciones

### 🔴 Error: "No executable found matching command 'dotnet-ef'"

**Causa**: La herramienta `dotnet-ef` no está instalada globalmente.

**Solución**:

```bash
# Instalar dotnet-ef globalmente
dotnet tool install --global dotnet-ef

# Verificar instalación
dotnet ef --version
```

**Resultado esperado**:
```
Entity Framework Core .NET Command-line Tools
10.0.5
```

### 🔴 Error: "Build failed" al ejecutar migrations

**Causa**: El proyecto tiene errores de compilación.

**Solución**:

```bash
# Primero asegurarse que compila
dotnet build

# Luego ejecutar migraciones
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```

### 🔴 Error: Migraciones se quedan "colgadas"

**Causa**: Proceso de dotnet colgado o timeout.

**Solución**:

```bash
# Matar procesos dotnet
taskkill /F /IM dotnet.exe

# Usar alternativa: EnsureCreated() en Program.cs
# Ya está implementado en el proyecto, solo ejecuta:
dotnet run
```

**Nota**: El proyecto usa `context.Database.EnsureCreated()` que crea la BD automáticamente sin necesidad de migraciones.

---

## 6. Errores de CORS

### 🔴 Error en navegador: "CORS policy blocked"

**Causa**: Frontend en dominio diferente al backend.

**Solución**: Agregar configuración de CORS en `Program.cs`

```csharp
// Después de: var builder = WebApplication.CreateBuilder(args);

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // URL de tu frontend
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ... resto del código

var app = builder.Build();

// ANTES de UseAuthentication
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();
```

**Ejemplo para producción**:

```csharp
policy.WithOrigins(
    "http://localhost:4200",      // Desarrollo
    "https://mi-app.com",          // Producción
    "https://www.mi-app.com"       // Producción con www
);
```

---

## 7. Verificación de Instalación

### ✅ Checklist de Requisitos

Verifica que tienes todo instalado correctamente:

```bash
# 1. Verificar .NET SDK 10
dotnet --version
# Debe mostrar: 10.x.x

# 2. Verificar dotnet-ef (opcional, para migraciones)
dotnet ef --version
# Debe mostrar: 10.x.x

# 3. Verificar que el proyecto compila
cd CrmService
dotnet build
# Debe mostrar: Compilación correcta. 0 Errores

# 4. Verificar paquetes instalados
dotnet list package
# Debe mostrar lista de paquetes sin errores

# 5. Ejecutar el proyecto
dotnet run
# Debe iniciar sin errores
```

### ✅ Salida Esperada al Ejecutar

Cuando ejecutas `dotnet run`, deberías ver:

```
🔄 Verificando/Creando base de datos...
✅ Base de datos lista.
🌱 Iniciando seeding de datos...
✅ 3 usuarios creados (Admin, Asesor, Auditor)
✅ 2 clientes creados
✅ 2 contactos creados
✅ 3 notas creadas
✅ 2 oportunidades de negocio creadas
✅ Seeding completado exitosamente!

📋 CREDENCIALES DE ACCESO:
---------------------------
👤 Admin:   admin@crm.com / Admin123!
👤 Asesor:  asesor@crm.com / Asesor123!
👤 Auditor: auditor@crm.com / Auditor123!

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### ✅ Verificar Swagger

Abre tu navegador en: **https://localhost:5001/swagger**

Deberías ver la interfaz de Swagger UI con todos los endpoints documentados.

---

## 📊 Tabla de Puertos

| Servicio | Puerto HTTP | Puerto HTTPS | URL |
|----------|-------------|--------------|-----|
| API REST | 5000 | 5001 | https://localhost:5001 |
| Swagger UI | 5000 | 5001 | https://localhost:5001/swagger |

---

## 🔍 Comandos Útiles de Diagnóstico

### Ver logs detallados

```bash
dotnet run --verbosity detailed
```

### Limpiar completamente el proyecto

```bash
# Eliminar archivos compilados
dotnet clean

# Eliminar carpetas bin y obj
rm -rf bin obj

# Restaurar y recompilar
dotnet restore
dotnet build
```

### Verificar conexión a base de datos

```bash
# Después de ejecutar el proyecto, verifica que exista crm.db
ls -la crm.db

# En Windows
dir crm.db
```

### Ver estructura de base de datos

Instala **DB Browser for SQLite**: https://sqlitebrowser.org/

Luego abre `crm.db` y verifica las tablas:
- Users
- Clients
- Contacts
- ClientNotes
- Opportunities

---

## 🆘 Solución Rápida: "Nada Funciona"

Si después de todo sigues teniendo problemas, prueba esto:

```bash
# 1. Eliminar TODO lo compilado y la base de datos
cd CrmService
rm -rf bin obj crm.db crm.db-shm crm.db-wal

# 2. Limpiar caché de NuGet
dotnet nuget locals all --clear

# 3. Restaurar paquetes
dotnet restore

# 4. Compilar
dotnet build

# 5. Ejecutar
dotnet run
```

Si aún así falla:

1. Verifica que tienes .NET 8 SDK instalado: `dotnet --version`
2. Clona el repositorio de nuevo en una carpeta diferente
3. Abre un issue en GitHub con el log completo del error

---

## 📞 Soporte

Si encuentras un error no documentado aquí:

1. **GitHub Issues**: https://github.com/Caibarguen10/CRM/issues
2. **Email**: caibarguen10@gmail.com
3. **Documentación adicional**: Ver README.md, SWAGGER-TESTING-GUIDE.md

---

## 📚 Documentos Relacionados

- **README.md** - Documentación técnica completa
- **README-COMERCIAL.md** - Guía comercial y casos de uso
- **SWAGGER-TESTING-GUIDE.md** - Guía de testing paso a paso
- **MIGRATIONS-AND-POLICIES.md** - Migraciones y políticas de autorización

---

**Última actualización**: Abril 2024  
**Versión del proyecto**: 1.0  
**Versión de .NET**: 10.0
