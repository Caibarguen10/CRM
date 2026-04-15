# 📸 Capturas de Pantalla - API CRM en Acción

Esta carpeta contiene **16 capturas de pantalla** que demuestran todas las funcionalidades del microservicio CRM. Las capturas muestran el API funcionando correctamente con autenticación JWT, autorización por roles, CRUD completo, y características empresariales.

---

## 📋 Índice de Capturas

### 🔐 **Autenticación y Autorización (1-2, 7-9)**

#### 01. Login Exitoso
**Archivo:** `01-login-exitoso.PNG`  
**Endpoint:** `POST /api/auth/login`  
**Demuestra:**
- Login exitoso con credenciales de Admin
- Token JWT generado correctamente
- Response incluye: token, username, email, role, expiresAt
- Código de respuesta: 200 OK

**Credenciales usadas:**
```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

---

#### 02. Autorización en Swagger
**Archivo:** `02-authorize-swagger.PNG`  
**Demuestra:**
- Modal "Available authorizations" de Swagger
- Token JWT pegado en el campo de autorización
- Configuración correcta para llamadas autenticadas
- Uso del botón "Authorize" 🔒

**Nota:** El token se pega SIN la palabra "Bearer" (Swagger lo agrega automáticamente)

---

#### 07. Error 403 Forbidden
**Archivo:** `07-forbidden-403.PNG`  
**Endpoint:** `POST /api/clients` (con rol Auditor)  
**Demuestra:**
- Sistema de permisos funcionando correctamente
- Usuario Auditor NO puede crear clientes (solo lectura)
- Política de autorización "ClientManagement" activa
- Código de respuesta: 403 Forbidden

---

#### 08. Error 401 Unauthorized
**Archivo:** `08-unauthorized-401.PNG`  
**Endpoint:** `GET /api/clients` (sin token)  
**Demuestra:**
- Endpoints protegidos requieren autenticación
- Request sin token JWT es rechazado
- Middleware de autenticación funcionando
- Código de respuesta: 401 Unauthorized

---

#### 09. Token Decodificado (jwt.io)
**Archivo:** `09-jwt-io-decoded.PNG`  
**Herramienta:** https://jwt.io/  
**Demuestra:**
- Estructura interna del token JWT
- **Header:** `{"alg": "HS256", "typ": "JWT"}`
- **Payload/Claims:**
  - `nameid`: 3 (User ID)
  - `unique_name`: "auditor"
  - `email`: "auditor@crm.com"
  - `role`: "Auditor"
  - `iss`: "CrmServiceAPI"
  - `aud`: "CrmServiceClient"
  - `exp`: Timestamp de expiración
- Token firmado con HMAC-SHA256

---

### 👥 **Gestión de Clientes (3-6, 10-11, 15)**

#### 03. Listar Clientes
**Archivo:** `03-get-clients-200.PNG`  
**Endpoint:** `GET /api/clients?page=1&pageSize=10`  
**Demuestra:**
- Listado de clientes con paginación
- 2 clientes en la base de datos:
  - Juan Pérez García (91234567)
  - María López Fernández (87654321)
- Metadatos de paginación:
  - `totalCount`: 2
  - `page`: 1
  - `pageSize`: 10
  - `totalPages`: 1
  - `hasPreviousPage`: false
  - `hasNextPage`: false
- Authorization header con Bearer token
- Código de respuesta: 200 OK

---

#### 04. Crear Cliente
**Archivo:** `04-post-client-201.PNG`  
**Endpoint:** `POST /api/clients`  
**Demuestra:**
- Creación exitosa de nuevo cliente
- Request body con datos del cliente:
  ```json
  {
    "documentNumber": "77654321",
    "fullName": "Laura Martínez",
    "email": "laura@example.com",
    "phone": "+57 321 9876543"
  }
  ```
- Response incluye el cliente creado con ID asignado
- Auditoría automática: `createdBy`, `createdAt` registrados
- Código de respuesta: 201 Created

---

#### 05. Soft Delete (Eliminar Cliente)
**Archivo:** `05-delete-client-204.PNG`  
**Endpoint:** `DELETE /api/clients/4`  
**Demuestra:**
- Eliminación lógica (soft delete) exitosa
- Solo usuarios con rol Admin pueden eliminar
- Cliente marcado como `IsDeleted = true` en la BD
- Auditoría: `deletedBy`, `deletedAt` registrados
- Código de respuesta: 204 No Content

---

#### 06. Verificar Cliente Eliminado
**Archivo:** `06-get-deleted-404.PNG`  
**Endpoint:** `GET /api/clients/4`  
**Demuestra:**
- Cliente eliminado NO aparece en consultas
- Filtro global de EF Core excluye registros con `IsDeleted = true`
- Soft delete funcionando correctamente
- Código de respuesta: 404 Not Found

---

#### 10. Filtrado por Email
**Archivo:** `10-filtrado-email.PNG`  
**Endpoint:** `GET /api/clients?email=maria@example.com`  
**Demuestra:**
- Filtrado dinámico funcionando
- Solo clientes que coinciden con el email son retornados
- Query parameters para búsqueda
- Optimización de consultas

---

#### 11. Paginación
**Archivo:** `11-paginacion.PNG`  
**Endpoint:** `GET /api/clients?pageNumber=1&pageSize=5`  
**Demuestra:**
- Sistema de paginación con parámetros configurables
- Metadatos completos de paginación
- Optimización para listados grandes
- Control del tamaño de respuesta

---

#### 15. Actualizar Cliente (PUT)
**Archivo:** `15-put-cliente.PNG`  
**Endpoint:** `PUT /api/clients/{id}`  
**Demuestra:**
- Actualización exitosa de cliente existente
- Auditoría automática: `updatedBy`, `updatedAt` actualizados
- Validación de datos en la actualización
- Código de respuesta: 200 OK

---

### 📞 **Contactos (12, 16)**

#### 12. Crear Contacto
**Archivo:** `12-crear-contacto.PNG`  
**Endpoint:** `POST /api/contacts`  
**Demuestra:**
- Creación de contacto asociado a un cliente
- Relación Client → Contact funcionando
- Request body:
  ```json
  {
    "clientId": 1,
    "name": "Ana García",
    "position": "Gerente de Compras",
    "email": "ana.garcia@empresa.com",
    "phone": "+57 310 1234567"
  }
  ```
- Foreign key constraint validado
- Código de respuesta: 201 Created

---

#### 16. Listar Contactos de un Cliente
**Archivo:** `16-get-contactos.PNG`  
**Endpoint:** `GET /api/contacts?clientId=1`  
**Demuestra:**
- Consulta de contactos por cliente
- Relación uno-a-muchos (Client → Contacts)
- Filtrado por cliente específico
- Navegación entre entidades relacionadas

---

### 📝 **Notas (13)**

#### 13. Crear Nota
**Archivo:** `13.crear-notas.PNG`  
**Endpoint:** `POST /api/notes`  
**Demuestra:**
- Creación de nota asociada a un cliente
- Sistema de seguimiento de interacciones
- Request body:
  ```json
  {
    "clientId": 1,
    "note": "Cliente interesado en plan premium"
  }
  ```
- Auditoría: quién y cuándo se registró la nota
- Código de respuesta: 201 Created

---

### 💰 **Oportunidades (14)**

#### 14. Crear Oportunidad
**Archivo:** `14-crear-oportunidades.PNG`  
**Endpoint:** `POST /api/opportunities`  
**Demuestra:**
- Creación de oportunidad de venta
- Seguimiento de pipeline de ventas
- Request body:
  ```json
  {
    "clientId": 1,
    "title": "Venta Plan Enterprise",
    "estimatedAmount": 5000.00,
    "status": "InProgress"
  }
  ```
- Gestión de montos y estados
- Código de respuesta: 201 Created

---

## 🎬 Flujo de Pruebas Demostrado

### **Flujo Completo (Orden de Capturas):**

```
1. Login (01) → Obtener token JWT
2. Authorize en Swagger (02) → Configurar autenticación
3. Listar clientes (03) → Ver datos existentes
4. Crear cliente (04) → Agregar nuevo registro
5. Filtrar por email (10) → Búsqueda específica
6. Paginación (11) → Control de listados grandes
7. Actualizar cliente (15) → Modificar datos
8. Crear contacto (12) → Relación con cliente
9. Listar contactos (16) → Consultar relaciones
10. Crear nota (13) → Registrar interacción
11. Crear oportunidad (14) → Pipeline de ventas
12. Soft delete (05) → Eliminar cliente
13. Verificar eliminado (06) → Confirmar soft delete
14. Error 403 (07) → Probar permisos
15. Error 401 (08) → Probar seguridad
16. jwt.io (09) → Validar token
```

---

## 🏆 Funcionalidades Demostradas

### ✅ Seguridad
- [x] Autenticación JWT
- [x] Autorización por roles (Admin, Asesor, Auditor)
- [x] Políticas de acceso granular
- [x] Validación de permisos (403)
- [x] Protección de endpoints (401)

### ✅ CRUD Completo
- [x] Create (POST /api/clients, contacts, notes, opportunities)
- [x] Read (GET con filtros y paginación)
- [x] Update (PUT /api/clients)
- [x] Delete (Soft delete)

### ✅ Características Empresariales
- [x] Auditoría automática (CreatedBy, UpdatedBy, DeletedBy)
- [x] Soft Delete (borrado lógico)
- [x] Paginación de resultados
- [x] Filtrado dinámico
- [x] Relaciones entre entidades (Foreign Keys)

### ✅ API REST Profesional
- [x] Swagger UI funcional
- [x] Códigos HTTP correctos (200, 201, 204, 401, 403, 404)
- [x] DTOs para separación de capas
- [x] Documentación interactiva

---

## 🎯 Uso para Portfolio/Demo

Estas capturas son ideales para:
- ✅ **Portfolio de desarrollador**: Demuestran habilidades en .NET, APIs REST, seguridad JWT
- ✅ **Presentaciones comerciales**: Muestran funcionalidad completa a clientes potenciales
- ✅ **Documentación técnica**: Guías visuales para usuarios del API
- ✅ **Pruebas de concepto**: Evidencia de que el sistema funciona correctamente

---

## 🛠️ Cómo Reproducir las Pruebas

### Requisitos:
- .NET 8 SDK instalado
- Git (para clonar el repositorio)

### Pasos:
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/Caibarguen10/CRM.git
   cd CRM/CrmService
   ```

2. Ejecutar la aplicación:
   ```bash
   dotnet run
   ```

3. Abrir Swagger:
   ```
   https://localhost:5001/swagger
   ```

4. Seguir la guía: `JWT-TESTING-QUICK-GUIDE.md`

---

## 📞 Usuarios de Prueba

| Username | Password     | Role    |
|----------|--------------|---------|
| admin    | Admin123!    | Admin   |
| asesor   | Asesor123!   | Asesor  |
| auditor  | Auditor123!  | Auditor |

---

## 📚 Documentación Relacionada

- **Guía de Testing JWT:** `JWT-TESTING-QUICK-GUIDE.md`
- **Guía de Testing Swagger:** `SWAGGER-TESTING-GUIDE.md`
- **Solución de Problemas:** `TROUBLESHOOTING.md`
- **Colección Postman:** `postman/CRM-Collection.json`
- **README Técnico:** `README.md`
- **README Comercial:** `README-COMERCIAL.md`

---

**Generado el:** 15 de Abril de 2026  
**Versión del API:** v1.0  
**.NET Version:** 8.0 (LTS)
