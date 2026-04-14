# 🚀 Guía de Migraciones y Políticas de Autorización

## 📊 PARTE 1: CREAR Y APLICAR MIGRACIONES DE BASE DE DATOS

### ¿Qué son las Migraciones de EF Core?

Las migraciones son una forma de mantener sincronizado el esquema de la base de datos con el modelo de datos de tu aplicación. Entity Framework Core genera código C# que crea/actualiza las tablas en la base de datos basándose en tus clases de dominio (entidades).

---

## 🔧 Comandos para Crear Migraciones

### **Paso 1: Instalar la herramienta EF Core CLI (si no la tienes)**

```bash
dotnet tool install --global dotnet-ef
```

Para verificar que está instalada:
```bash
dotnet ef --version
```

Deberías ver algo como: `Entity Framework Core .NET Command-line Tools 9.0.0`

---

### **Paso 2: Navegar a la carpeta del proyecto**

```bash
cd "C:\Users\calo-\OneDrive\Documentos\Proyecto Angular\App CRM Simple\CrmService"
```

---

### **Paso 3: Crear la migración inicial**

Este comando analiza tus entidades (Client, Contact, ClientNote, Opportunity, User) y genera el código para crear las tablas:

```bash
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```

**Explicación de parámetros**:
- `migrations add`: Comando para crear una nueva migración
- `InitialCreate`: Nombre de la migración (puedes usar cualquier nombre descriptivo)
- `--output-dir Data/Migrations`: Carpeta donde se guardarán los archivos de migración

**Resultado**: Se crearán 3 archivos en `Data/Migrations/`:
- `<timestamp>_InitialCreate.cs`: Métodos `Up()` (aplicar) y `Down()` (revertir)
- `<timestamp>_InitialCreate.Designer.cs`: Metadata de la migración
- `AppDbContextModelSnapshot.cs`: Snapshot del modelo actual

---

### **Paso 4: Revisar la migración generada (Opcional)**

Abre el archivo `<timestamp>_InitialCreate.cs` para ver el SQL que se ejecutará:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Clients",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                .Annotation("Sqlite:Autoincrement", true),
            Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
            DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
            Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            // ... más columnas
        });
    
    // Más tablas: Contacts, ClientNotes, Opportunities, Users
}
```

---

### **Paso 5: Aplicar la migración a la base de datos**

Este comando ejecuta las migraciones pendientes y crea el archivo `crm.db` con todas las tablas:

```bash
dotnet ef database update
```

**Resultado**: 
- Se crea el archivo `crm.db` en la raíz del proyecto
- Se crean las tablas: `Clients`, `Contacts`, `ClientNotes`, `Opportunities`, `Users`
- Se crea la tabla `__EFMigrationsHistory` que rastrea las migraciones aplicadas

**Output esperado**:
```
Build started...
Build succeeded.
Applying migration '20260414103000_InitialCreate'.
Done.
```

---

### **Paso 6: Verificar que la base de datos se creó correctamente**

Puedes usar un visor de SQLite para inspeccionar las tablas:

**Opción 1 - DB Browser for SQLite (Recomendado)**:
1. Descargar: https://sqlitebrowser.org/
2. Abrir: File → Open Database → Seleccionar `crm.db`
3. Ver tablas en pestaña "Database Structure"

**Opción 2 - Extensión de VS Code**:
1. Instalar extensión: "SQLite" por alexcvzz
2. Ctrl+Shift+P → "SQLite: Open Database"
3. Seleccionar `crm.db`

**Opción 3 - Comando CLI**:
```bash
sqlite3 crm.db
.tables
.schema Clients
.exit
```

---

## 🔄 Comandos Adicionales de Migraciones

### Crear una nueva migración (después de cambiar entidades)

Ejemplo: Agregaste un nuevo campo `Website` a la entidad `Client`.

```bash
dotnet ef migrations add AddWebsiteToClient
dotnet ef database update
```

### Revertir la última migración aplicada

```bash
dotnet ef database update <NombreMigracionAnterior>
```

Ejemplo: Volver a la migración anterior a `AddWebsiteToClient`:
```bash
dotnet ef migrations list  # Ver lista de migraciones
dotnet ef database update InitialCreate  # Volver a InitialCreate
```

### Eliminar la última migración (NO aplicada aún)

```bash
dotnet ef migrations remove
```

⚠️ **Solo funciona si la migración NO ha sido aplicada con `database update`**

### Ver lista de todas las migraciones

```bash
dotnet ef migrations list
```

### Generar script SQL de las migraciones

Si necesitas el SQL puro sin ejecutarlo:

```bash
dotnet ef migrations script --output migrations.sql
```

### Recrear la base de datos desde cero

⚠️ **CUIDADO: Esto borra TODOS los datos**

```bash
dotnet ef database drop --force
dotnet ef database update
```

---

## 🔐 PARTE 2: POLÍTICAS DE AUTORIZACIÓN

### ¿Qué son las Políticas de Autorización?

Las políticas son requisitos de autorización reutilizables que se definen en `Program.cs` y se aplican en los controllers con el atributo `[Authorize(Policy = "NombrePolítica")]`.

---

## 📋 Políticas Configuradas en el Proyecto

El archivo `Program.cs` ahora incluye estas políticas:

```csharp
builder.Services.AddAuthorization(options =>
{
    // 1. AdminOnly: Solo Admin
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    // 2. AdminOrAsesor: Admin o Asesor
    options.AddPolicy("AdminOrAsesor", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // 3. AllRoles: Cualquier usuario autenticado
    options.AddPolicy("AllRoles", policy => 
        policy.RequireRole("Admin", "Asesor", "Auditor"));
    
    // 4. ClientManagement: Admin y Asesor (gestión de clientes)
    options.AddPolicy("ClientManagement", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // 5. ReadOnly: Todos pueden leer
    options.AddPolicy("ReadOnly", policy => 
        policy.RequireRole("Admin", "Asesor", "Auditor"));
    
    // 6. NoteManagement: Admin y Asesor (documentar interacciones)
    options.AddPolicy("NoteManagement", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // 7. DeletePermission: Solo Admin puede eliminar
    options.AddPolicy("DeletePermission", policy => 
        policy.RequireRole("Admin"));
});
```

---

## 🛡️ Cómo Aplicar Políticas en Controllers

### **Ejemplo 1: ClientsController con Políticas**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrmService.Services;
using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ← Requiere autenticación en TODOS los endpoints
public class ClientsController : ControllerBase
{
    private readonly IClientService _service;

    public ClientsController(IClientService service)
    {
        _service = service;
    }

    // GET /api/clients
    // Política: Todos los roles pueden leer
    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? documentNumber = null,
        [FromQuery] string? fullName = null,
        [FromQuery] string? email = null)
    {
        var pagination = new PaginationParams { Page = page, PageSize = pageSize };
        var filters = new ClientFilterDto
        {
            DocumentNumber = documentNumber,
            FullName = fullName,
            Email = email
        };

        var result = await _service.GetAllAsync(pagination, filters);
        return Ok(ApiResponse<PagedResult<ClientDto>>.Ok(result));
    }

    // GET /api/clients/5
    // Política: Todos los roles pueden leer
    [HttpGet("{id}")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClientDto>.Ok(result));
    }

    // POST /api/clients
    // Política: Solo Admin puede crear clientes
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(ApiResponse<ClientDto>.Ok(result, "Cliente creado correctamente."));
    }

    // PUT /api/clients/5
    // Política: Solo Admin puede actualizar clientes
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateClientDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result == null)
            return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<ClientDto>.Ok(result, "Cliente actualizado correctamente."));
    }

    // DELETE /api/clients/5
    // Política: Solo Admin puede eliminar (soft delete)
    [HttpDelete("{id}")]
    [Authorize(Policy = "DeletePermission")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
        return Ok(ApiResponse<string>.Ok(null!, "Cliente eliminado correctamente."));
    }
}
```

---

### **Ejemplo 2: NotesController con Políticas**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrmService.Services;
using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación
public class NotesController : ControllerBase
{
    private readonly INoteService _service;

    public NotesController(INoteService service)
    {
        _service = service;
    }

    // POST /api/notes
    // Política: Admin y Asesor pueden crear notas (documentar interacciones)
    [HttpPost]
    [Authorize(Policy = "NoteManagement")]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return Ok(ApiResponse<int>.Ok(id, "Nota creada correctamente."));
    }
}
```

---

### **Ejemplo 3: ContactsController con Políticas**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrmService.Services;
using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IContactService _service;

    public ContactsController(IContactService service)
    {
        _service = service;
    }

    // GET /api/contacts/client/5
    // Política: Todos los roles pueden leer contactos
    [HttpGet("client/{clientId:int}")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<IActionResult> GetByClient(int clientId)
    {
        var result = await _service.GetByClientIdAsync(clientId);
        return Ok(ApiResponse<List<ContactDto>>.Ok(result));
    }

    // POST /api/contacts
    // Política: Solo Admin puede crear contactos
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateContactDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(ApiResponse<ContactDto>.Ok(result, "Contacto creado correctamente."));
    }
}
```

---

### **Ejemplo 4: OpportunitiesController con Políticas**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrmService.Services;
using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityService _service;

    public OpportunitiesController(IOpportunityService service)
    {
        _service = service;
    }

    // POST /api/opportunities
    // Política: Admin y Asesor pueden crear oportunidades
    [HttpPost]
    [Authorize(Policy = "AdminOrAsesor")]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return Ok(ApiResponse<int>.Ok(id, "Oportunidad creada correctamente."));
    }
}
```

---

## 📊 Matriz de Permisos con Políticas

| Endpoint | Política Aplicada | Admin | Asesor | Auditor |
|----------|-------------------|-------|--------|---------|
| `GET /api/clients` | `ReadOnly` | ✅ | ✅ | ✅ |
| `GET /api/clients/{id}` | `ReadOnly` | ✅ | ✅ | ✅ |
| `POST /api/clients` | `AdminOnly` | ✅ | ❌ | ❌ |
| `PUT /api/clients/{id}` | `AdminOnly` | ✅ | ❌ | ❌ |
| `DELETE /api/clients/{id}` | `DeletePermission` | ✅ | ❌ | ❌ |
| `GET /api/contacts/client/{id}` | `ReadOnly` | ✅ | ✅ | ✅ |
| `POST /api/contacts` | `AdminOnly` | ✅ | ❌ | ❌ |
| `POST /api/notes` | `NoteManagement` | ✅ | ✅ | ❌ |
| `POST /api/opportunities` | `AdminOrAsesor` | ✅ | ✅ | ❌ |

---

## 🧪 Probar las Políticas con Postman

### **Paso 1: Registrar usuarios con diferentes roles**

```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "admin",
  "email": "admin@crm.com",
  "password": "Admin123!",
  "role": "Admin"
}
```

```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "asesor",
  "email": "asesor@crm.com",
  "password": "Asesor123!",
  "role": "Asesor"
}
```

```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "auditor",
  "email": "auditor@crm.com",
  "password": "Auditor123!",
  "role": "Auditor"
}
```

---

### **Paso 2: Login y obtener tokens**

Login como **Admin**:
```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!"
}
```

Copiar el `token` de la respuesta.

---

### **Paso 3: Probar endpoint con política**

Intentar crear un cliente con token de **Auditor** (debería fallar):

```http
POST http://localhost:5000/api/clients
Authorization: Bearer {token_auditor}
Content-Type: application/json

{
  "name": "Test Client",
  "documentNumber": "12345678",
  "email": "test@test.com"
}
```

**Respuesta esperada**: `403 Forbidden`

Intentar con token de **Admin** (debería funcionar):

```http
POST http://localhost:5000/api/clients
Authorization: Bearer {token_admin}
Content-Type: application/json

{
  "name": "Test Client",
  "documentNumber": "12345678",
  "email": "test@test.com"
}
```

**Respuesta esperada**: `200 OK` con el cliente creado

---

## 🎯 Resumen de Comandos Clave

### Migraciones:
```bash
# 1. Crear migración inicial
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# 2. Aplicar migración (crear BD)
dotnet ef database update

# 3. Ver lista de migraciones
dotnet ef migrations list

# 4. Revertir a migración anterior
dotnet ef database update <NombreMigración>

# 5. Eliminar última migración (no aplicada)
dotnet ef migrations remove

# 6. Recrear BD desde cero (BORRA DATOS)
dotnet ef database drop --force
dotnet ef database update
```

### Ejecutar el proyecto:
```bash
dotnet run
```

### Acceder a Swagger UI:
```
https://localhost:5001/swagger
```

---

## ✅ Checklist Final

- [ ] Instalar `dotnet-ef` tool
- [ ] Crear migración `InitialCreate`
- [ ] Aplicar migración con `database update`
- [ ] Verificar que `crm.db` se creó correctamente
- [ ] Políticas de autorización agregadas en `Program.cs`
- [ ] Atributos `[Authorize(Policy = "...")]` aplicados en controllers
- [ ] Registrar usuarios de prueba (Admin, Asesor, Auditor)
- [ ] Probar endpoints con tokens de diferentes roles
- [ ] Verificar que las políticas funcionan (403 Forbidden cuando corresponde)

---

**¡Listo! Tu CRM ahora tiene migraciones configuradas y políticas de autorización completas.** 🎉
