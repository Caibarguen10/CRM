# 🧪 Guía Completa de Testing con Swagger y JWT

Esta guía te muestra **paso a paso** cómo probar el microservicio CRM usando Swagger UI y obtener capturas para documentación o demos.

---

## 📋 Tabla de Contenidos

1. [Ejecutar el Proyecto](#1-ejecutar-el-proyecto)
2. [Acceder a Swagger UI](#2-acceder-a-swagger-ui)
3. [Obtener Token JWT (Login)](#3-obtener-token-jwt-login)
4. [Decodificar y Entender el Token JWT](#4-decodificar-y-entender-el-token-jwt)
5. [Configurar Autorización en Swagger](#5-configurar-autorización-en-swagger)
6. [Probar Endpoints Protegidos](#6-probar-endpoints-protegidos)
7. [Testing por Roles](#7-testing-por-roles)
8. [Capturas Recomendadas para Mockup](#8-capturas-recomendadas-para-mockup)

---

## 1. Ejecutar el Proyecto

### Paso 1.1: Navegar al directorio del proyecto
```bash
git clone https://github.com/Caibarguen10/CRM.git
cd CRM/CrmService
```

### Paso 1.2: Ejecutar el proyecto
```bash
dotnet run
```

**Salida esperada en consola**:
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
```

**✅ CAPTURA 1**: Screenshot de la consola mostrando el seeding exitoso

---

## 2. Acceder a Swagger UI

### Paso 2.1: Abrir navegador
```
https://localhost:5001/swagger
```

O alternativamente (HTTP):
```
http://localhost:5000/swagger
```

### Paso 2.2: Vista principal de Swagger

Verás la interfaz de Swagger UI con estos grupos de endpoints:

```
┌─────────────────────────────────────────────────────┐
│ CRM Service API v1                                  │
│ API REST para gestión de relaciones con clientes   │
├─────────────────────────────────────────────────────┤
│ 🔐 Auth                                             │
│   POST /api/auth/register                          │
│   POST /api/auth/login                             │
├─────────────────────────────────────────────────────┤
│ 👥 Clients                                          │
│   GET    /api/clients                              │
│   POST   /api/clients                              │
│   GET    /api/clients/{id}                         │
│   PUT    /api/clients/{id}                         │
│   DELETE /api/clients/{id}                         │
├─────────────────────────────────────────────────────┤
│ 📞 Contacts                                         │
│   GET  /api/contacts/client/{clientId}             │
│   POST /api/contacts                               │
├─────────────────────────────────────────────────────┤
│ 📝 Notes                                            │
│   POST /api/notes                                  │
├─────────────────────────────────────────────────────┤
│ 💰 Opportunities                                    │
│   POST /api/opportunities                          │
└─────────────────────────────────────────────────────┘
```

**✅ CAPTURA 2**: Screenshot de Swagger UI mostrando todos los endpoints

---

## 3. Obtener Token JWT (Login)

### Paso 3.1: Expandir el endpoint de login

1. Clic en `POST /api/auth/login`
2. Clic en **"Try it out"**

### Paso 3.2: Ingresar credenciales

En el campo **Request body**, ingresa:

```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

**✅ CAPTURA 3**: Screenshot mostrando el request body con las credenciales

### Paso 3.3: Ejecutar la petición

Clic en el botón **"Execute"**

### Paso 3.4: Ver la respuesta

**Respuesta exitosa (200 OK)**:

```json
{
  "success": true,
  "message": "Login exitoso",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBjcm0uY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3MTMxMDMyMDAsImlzcyI6IkNybVNlcnZpY2VBUEkiLCJhdWQiOiJDcm1TZXJ2aWNlQ2xpZW50In0.xK8vZ9mW2nB5cJ7gH4fD3kL6mN8pQ9rS1tU2vW3xY4z",
    "username": "admin",
    "email": "admin@crm.com",
    "role": "Admin"
  }
}
```

### Paso 3.5: Copiar el token

**IMPORTANTE**: Copia el valor completo del campo `token` (sin las comillas). Este token tiene el formato:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRw...xY4z
```

**✅ CAPTURA 4**: Screenshot de la respuesta del login mostrando el token JWT

---

## 4. Decodificar y Entender el Token JWT

### Paso 4.1: Ir a jwt.io

Abre en tu navegador: **https://jwt.io**

### Paso 4.2: Pegar el token

En la sección **"Encoded"**, pega el token que copiaste.

### Paso 4.3: Ver el payload decodificado

**Header (Algoritmo de firma)**:
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims del usuario)**:
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "1",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "admin@crm.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "exp": 1713103200,
  "iss": "CrmServiceAPI",
  "aud": "CrmServiceClient"
}
```

**Explicación de los Claims**:
- `nameidentifier`: ID del usuario en la base de datos (1)
- `name`: Username utilizado para auditoría automática
- `emailaddress`: Email del usuario
- `role`: Rol del usuario (Admin, Asesor, Auditor)
- `exp`: Fecha de expiración del token (Unix timestamp)
- `iss`: Issuer (quién emitió el token)
- `aud`: Audience (para quién es el token)

**✅ CAPTURA 5**: Screenshot de jwt.io mostrando el token decodificado

---

## 5. Configurar Autorización en Swagger

### Paso 5.1: Localizar el botón "Authorize"

En la parte superior derecha de Swagger UI, verás un botón **"Authorize"** con un candado 🔒

### Paso 5.2: Hacer clic en "Authorize"

Se abrirá un modal con el título **"Available authorizations"**

### Paso 5.3: Ingresar el token

En el campo **"Value"**, ingresa:

```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRw...xY4z
```

**IMPORTANTE**: Debe incluir la palabra **`Bearer`** seguida de un espacio y luego el token.

### Paso 5.4: Hacer clic en "Authorize"

El candado cambiará de abierto 🔓 a cerrado 🔒, indicando que estás autenticado.

### Paso 5.5: Cerrar el modal

Clic en **"Close"**

**✅ CAPTURA 6**: Screenshot del modal de autorización con el token ingresado

---

## 6. Probar Endpoints Protegidos

### Test 1: Obtener Lista de Clientes

#### Paso 6.1: Expandir `GET /api/clients`

Clic en el endpoint y luego en **"Try it out"**

#### Paso 6.2: Configurar parámetros (opcional)

```
page: 1
pageSize: 10
documentNumber: (dejar vacío)
fullName: (dejar vacío)
email: (dejar vacío)
```

#### Paso 6.3: Ejecutar

Clic en **"Execute"**

#### Paso 6.4: Ver respuesta

**Respuesta esperada (200 OK)**:

```json
{
  "success": true,
  "message": null,
  "data": {
    "items": [
      {
        "id": 1,
        "documentNumber": "12345678",
        "fullName": "Juan Pérez García",
        "email": "juan.perez@email.com",
        "phone": "+34 600 123 456",
        "address": "Calle Mayor 123, 28013 Madrid, España",
        "createdAt": "2024-04-14T10:30:00Z",
        "createdBy": "admin"
      },
      {
        "id": 2,
        "documentNumber": "87654321",
        "fullName": "María López Fernández",
        "email": "maria.lopez@email.com",
        "phone": "+34 600 654 321",
        "address": "Avenida Diagonal 456, 08036 Barcelona, España",
        "createdAt": "2024-04-14T10:30:00Z",
        "createdBy": "admin"
      }
    ],
    "totalCount": 2,
    "page": 1,
    "pageSize": 10
  }
}
```

**✅ CAPTURA 7**: Screenshot de la respuesta de GET /api/clients

---

### Test 2: Crear un Nuevo Cliente

#### Paso 6.5: Expandir `POST /api/clients`

Clic en **"Try it out"**

#### Paso 6.6: Ingresar datos del cliente

```json
{
  "documentNumber": "11223344",
  "fullName": "Carlos Rodríguez Sánchez",
  "email": "carlos.rodriguez@email.com",
  "phone": "+34 600 999 888",
  "address": "Gran Vía 789, 28013 Madrid, España"
}
```

#### Paso 6.7: Ejecutar

Clic en **"Execute"**

#### Paso 6.8: Ver respuesta

**Respuesta esperada (200 OK)**:

```json
{
  "success": true,
  "message": "Cliente creado correctamente.",
  "data": {
    "id": 3,
    "documentNumber": "11223344",
    "fullName": "Carlos Rodríguez Sánchez",
    "email": "carlos.rodriguez@email.com",
    "phone": "+34 600 999 888",
    "address": "Gran Vía 789, 28013 Madrid, España",
    "createdAt": "2024-04-14T12:45:30Z",
    "createdBy": "admin"
  }
}
```

**Nota**: Observa que `createdBy` es automáticamente "admin" (obtenido del token JWT)

**✅ CAPTURA 8**: Screenshot del POST /api/clients con la respuesta exitosa

---

### Test 3: Crear Nota para un Cliente

#### Paso 6.9: Expandir `POST /api/notes`

#### Paso 6.10: Ingresar datos de la nota

```json
{
  "clientId": 1,
  "content": "Cliente solicitó cotización para servicio de consultoría CRM. Enviar propuesta antes del viernes."
}
```

#### Paso 6.11: Ejecutar y ver respuesta

**Respuesta esperada (200 OK)**:

```json
{
  "success": true,
  "message": "Nota creada correctamente.",
  "data": 4
}
```

El valor `data: 4` es el ID de la nota creada.

**✅ CAPTURA 9**: Screenshot del POST /api/notes

---

### Test 4: Crear Oportunidad de Venta

#### Paso 6.12: Expandir `POST /api/opportunities`

#### Paso 6.13: Ingresar datos de la oportunidad

```json
{
  "clientId": 3,
  "title": "Implementación CRM Personalizado",
  "description": "Cliente requiere sistema CRM adaptado a su flujo de ventas con integración a ERP existente",
  "amount": 35000.00,
  "stage": "Proposal",
  "closeDate": "2024-06-30T00:00:00Z"
}
```

**Valores válidos para `stage`**:
- `Lead` - Prospecto inicial
- `Qualification` - Calificación
- `Proposal` - Propuesta enviada
- `Negotiation` - Negociación
- `Closed-Won` - Venta cerrada (ganada)
- `Closed-Lost` - Venta cerrada (perdida)

#### Paso 6.14: Ejecutar y ver respuesta

**Respuesta esperada (200 OK)**:

```json
{
  "success": true,
  "message": "Oportunidad creada correctamente.",
  "data": 3
}
```

**✅ CAPTURA 10**: Screenshot del POST /api/opportunities

---

## 7. Testing por Roles

### Probar con Rol "Asesor"

#### Paso 7.1: Hacer logout (limpiar autorización)

1. Clic en **"Authorize"**
2. Clic en **"Logout"**
3. Clic en **"Close"**

#### Paso 7.2: Hacer login como Asesor

```json
{
  "username": "asesor",
  "password": "Asesor123!"
}
```

Copiar el nuevo token y autorizar con él.

#### Paso 7.3: Intentar eliminar un cliente

Expandir `DELETE /api/clients/{id}`, ingresar `id: 1` y ejecutar.

**Respuesta esperada (403 Forbidden)**:

```json
{
  "success": false,
  "message": "Acceso denegado. No tienes permisos para realizar esta acción.",
  "data": null
}
```

**Explicación**: El endpoint DELETE requiere la política `DeletePermission` que solo Admin tiene.

**✅ CAPTURA 11**: Screenshot mostrando error 403 Forbidden para Asesor intentando eliminar

---

### Probar con Rol "Auditor"

#### Paso 7.4: Hacer login como Auditor

```json
{
  "username": "auditor",
  "password": "Auditor123!"
}
```

#### Paso 7.5: Intentar crear un cliente

Expandir `POST /api/clients` e intentar crear uno.

**Respuesta esperada (403 Forbidden)**:

```json
{
  "success": false,
  "message": "Acceso denegado. No tienes permisos para realizar esta acción.",
  "data": null
}
```

**Explicación**: POST /api/clients requiere la política `ClientManagement` (Admin, Asesor). Auditor solo tiene permisos de lectura.

#### Paso 7.6: Intentar ver clientes (debería funcionar)

Expandir `GET /api/clients` y ejecutar.

**Respuesta esperada (200 OK)**: Lista completa de clientes

**Explicación**: GET /api/clients usa la política `ReadOnly` que incluye a Auditor.

**✅ CAPTURA 12**: Screenshot mostrando que Auditor SÍ puede hacer GET pero NO POST

---

## 8. Capturas Recomendadas para Mockup

### Lista de Capturas Esenciales

Para crear un mockup/demo profesional, captura:

#### 🎯 Sección: Inicio y Configuración
1. **Consola al ejecutar dotnet run** - Mostrando el seeding automático
2. **Página principal de Swagger** - Vista general de todos los endpoints
3. **Modal de autorización** - Con el token Bearer configurado

#### 🔐 Sección: Autenticación
4. **POST /api/auth/login (request)** - Con credenciales de admin
5. **POST /api/auth/login (response)** - Mostrando el token JWT generado
6. **jwt.io** - Token decodificado mostrando los claims

#### 👥 Sección: Gestión de Clientes
7. **GET /api/clients (response)** - Lista de clientes con paginación
8. **POST /api/clients (request + response)** - Creación exitosa
9. **GET /api/clients/{id} (response)** - Detalle de un cliente específico
10. **PUT /api/clients/{id} (response)** - Actualización exitosa

#### 📝 Sección: Notas y Seguimiento
11. **POST /api/notes (request + response)** - Nota creada
12. **Campo `createdBy`** - Destacar que se llena automáticamente

#### 🛡️ Sección: Seguridad y Roles
13. **DELETE como Asesor → 403** - Mostrando restricción por rol
14. **POST como Auditor → 403** - Mostrando restricción de escritura
15. **GET como Auditor → 200** - Mostrando permiso de lectura

#### 💰 Sección: Oportunidades
16. **POST /api/opportunities (request + response)** - Con monto decimal

---

## 9. Comandos cURL Equivalentes

Si prefieres usar cURL en lugar de Swagger:

### Login
```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'
```

### Obtener Clientes (con token)
```bash
curl -X GET "https://localhost:5001/api/clients?page=1&pageSize=10" \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```

### Crear Cliente
```bash
curl -X POST "https://localhost:5001/api/clients" \
  -H "Authorization: Bearer TU_TOKEN_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "documentNumber": "11223344",
    "fullName": "Carlos Rodríguez",
    "email": "carlos@email.com",
    "phone": "+34 600 999 888",
    "address": "Gran Vía 789, Madrid"
  }'
```

### Crear Nota
```bash
curl -X POST "https://localhost:5001/api/notes" \
  -H "Authorization: Bearer TU_TOKEN_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": 1,
    "content": "Cliente interesado en servicio premium"
  }'
```

---

## 10. Troubleshooting

### Problema: "401 Unauthorized"

**Causa**: Token no configurado o expirado

**Solución**:
1. Verificar que hiciste clic en "Authorize" en Swagger
2. Verificar que el token incluye la palabra "Bearer" al inicio
3. Hacer login nuevamente para obtener un token fresco

---

### Problema: "403 Forbidden"

**Causa**: El usuario no tiene el rol necesario para esa operación

**Solución**:
- Verificar la matriz de permisos en el README
- Hacer login con un usuario que tenga el rol adecuado
- Ejemplo: Para DELETE, necesitas rol Admin

---

### Problema: "La base de datos no se crea"

**Causa**: Error en la ejecución del seeder

**Solución**:
```bash
# Eliminar la base de datos existente
rm crm.db

# Ejecutar de nuevo
dotnet run
```

---

### Problema: "El token está en formato inválido"

**Causa**: Token copiado incorrectamente o con caracteres extra

**Solución**:
- Asegúrate de copiar solo el valor del campo `token` (sin comillas)
- Debe empezar con: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.`
- No debe tener espacios ni saltos de línea en el medio

---

## 11. Siguientes Pasos

### Para Desarrollo
- [ ] Agregar más endpoints (GET contactos, GET notas por cliente)
- [ ] Implementar filtros avanzados
- [ ] Agregar exportación a Excel/PDF
- [ ] Implementar paginación en todas las colecciones

### Para Producción
- [ ] Cambiar `Jwt:SecretKey` en appsettings.json por una clave segura
- [ ] Migrar de SQLite a SQL Server o PostgreSQL
- [ ] Configurar CORS para tu frontend
- [ ] Configurar HTTPS con certificado SSL válido
- [ ] Implementar rate limiting
- [ ] Configurar logging a Application Insights o ELK

### Para Testing
- [ ] Crear tests unitarios con xUnit
- [ ] Crear tests de integración
- [ ] Configurar GitHub Actions para CI/CD
- [ ] Agregar Swagger annotations para mejor documentación

---

## 📞 Soporte

Si encuentras problemas durante el testing, abre un issue en:

👉 **https://github.com/Caibarguen10/CRM/issues**

---

**¡Feliz testing! 🚀**
