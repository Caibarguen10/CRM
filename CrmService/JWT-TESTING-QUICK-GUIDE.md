# Guía Rápida: Cómo Probar JWT en Swagger

## Problema Común: 401 Unauthorized

Si obtienes 401 Unauthorized después de hacer login, es porque **el token no se está enviando correctamente**.

---

## Pasos Correctos para Probar JWT en Swagger

### 1. Iniciar la Aplicación

```bash
cd "C:\Users\calo-\Documents\App CRM Simple\CrmService"
dotnet run
```

Espera el mensaje:
```
✅ Base de datos lista.
Now listening on: https://localhost:5001
```

### 2. Abrir Swagger UI

Navega a: **https://localhost:5001/swagger**

---

### 3. Hacer Login (Obtener Token)

1. Expande el endpoint: **POST /api/auth/login**
2. Haz clic en "Try it out"
3. Ingresa las credenciales en el Request body:

```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

4. Haz clic en "Execute"
5. Verifica que la respuesta sea **200 OK**
6. **COPIA EL TOKEN** del response body:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AY3JtLmNvbSIsInJvbGUiOiJBZG1pbiIsIm5iZiI6MTcxMzExNjQwMCwiZXhwIjoxNzEzMTIwMDAwLCJpc3MiOiJDcm1TZXJ2aWNlQVBJIiwiYXVkIjoiQ3JtU2VydmljZUNsaWVudCJ9.XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  "username": "admin",
  "email": "admin@crm.com",
  "role": "Admin",
  "expiresAt": "2024-04-14T20:00:00Z"
}
```

📋 **Copia SOLO el valor de `"token"`** (el string largo que empieza con `eyJ...`)

---

### 4. Autorizar en Swagger (PASO CRÍTICO)

1. **Haz clic en el botón verde "Authorize"** en la parte superior derecha de Swagger UI
2. En el modal que aparece, verás un campo de texto bajo "Bearer (apiKey)"
3. **PEGA SOLO EL TOKEN** (sin la palabra "Bearer", sin comillas)

   ✅ **CORRECTO**:
   ```
   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AY3JtLmNvbSIsInJvbGUiOiJBZG1pbiIsIm5iZiI6MTcxMzExNjQwMCwiZXhwIjoxNzEzMTIwMDAwLCJpc3MiOiJDcm1TZXJ2aWNlQVBJIiwiYXVkIjoiQ3JtU2VydmljZUNsaWVudCJ9.XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
   ```

   ❌ **INCORRECTO**:
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

   ❌ **INCORRECTO**:
   ```
   "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   ```

4. Haz clic en **"Authorize"**
5. Haz clic en **"Close"**

📌 **Swagger agregará automáticamente "Bearer" cuando haga las peticiones**

---

### 5. Probar un Endpoint Protegido

1. Expande el endpoint: **GET /api/clients**
2. Haz clic en "Try it out"
3. Haz clic en "Execute"
4. Verifica que obtengas **200 OK** con la lista de clientes

---

## Verificar que el Token es Válido

### Opción A: jwt.io (Recomendado)

1. Ve a: **https://jwt.io/**
2. Pega tu token en la sección "Encoded"
3. Verifica en "Decoded" que veas:
   - **Header**: `{"alg": "HS256", "typ": "JWT"}`
   - **Payload**: Claims con `nameid`, `unique_name`, `email`, `role`
   - **Issuer**: `CrmServiceAPI`
   - **Audience**: `CrmServiceClient`
   - **Expiration**: Fecha futura (no expirado)

### Opción B: Postman (Alternativa)

1. Abre Postman
2. Crea un nuevo request: **GET** `https://localhost:5001/api/clients`
3. Ve a la pestaña **"Authorization"**
4. Selecciona **"Bearer Token"**
5. Pega el token
6. Haz clic en **"Send"**
7. Deberías obtener **200 OK** con la lista de clientes

---

## Usuarios Disponibles para Pruebas

| Username | Password     | Role    | Permisos                          |
|----------|--------------|---------|-----------------------------------|
| admin    | Admin123!    | Admin   | Acceso total (CRUD + Delete)      |
| asesor   | Asesor123!   | Asesor  | CRUD clientes/notas (sin Delete)  |
| auditor  | Auditor123!  | Auditor | Solo lectura (GET)                |

---

## Solución de Problemas

### Error: 401 Unauthorized después de autorizar

**Causa**: Token mal formado, expirado, o Swagger no lo está enviando correctamente.

**Soluciones**:
1. Verifica que pegaste **SOLO el token** (sin "Bearer", sin comillas)
2. Verifica que el token no haya expirado (válido por 60 minutos)
3. Haz logout y login de nuevo para obtener un token fresco
4. Verifica en las DevTools del navegador (F12 → Network) que el header incluya:
   ```
   Authorization: Bearer eyJhbGc...
   ```

### Error: 403 Forbidden

**Causa**: El usuario está autenticado pero no tiene permisos.

**Ejemplo**:
- Usuario `auditor` intenta **POST** /api/clients → 403 (solo puede hacer GET)
- Usuario `asesor` intenta **DELETE** /api/clients/1 → 403 (solo Admin puede eliminar)

**Solución**: Usa un usuario con los permisos correctos (ver tabla arriba).

### Error: Token expirado

**Causa**: El token tiene 60 minutos de validez.

**Solución**: Haz login de nuevo para obtener un token nuevo.

---

## Flujo Completo de Ejemplo

```bash
# 1. Login como Admin
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin123!"
}
→ Response: 200 OK + token

# 2. Autorizar en Swagger (botón verde "Authorize")
→ Pegar token (sin "Bearer")

# 3. Listar clientes
GET /api/clients
→ Response: 200 OK + array de clientes

# 4. Crear cliente
POST /api/clients
{
  "documentNumber": "77654321",
  "fullName": "Laura Martínez",
  "email": "laura@example.com",
  "phone": "+57 321 9876543"
}
→ Response: 201 Created

# 5. Soft Delete (solo Admin)
DELETE /api/clients/4
→ Response: 204 No Content

# 6. Verificar soft delete
GET /api/clients/4
→ Response: 404 Not Found (fue marcado como eliminado)
```

---

## Capturas de Pantalla Recomendadas

Después de verificar que todo funciona, genera estas capturas:

1. ✅ **Login exitoso** (POST /api/auth/login → 200 OK con token)
2. ✅ **Botón Authorize en Swagger** (modal con token pegado)
3. ✅ **GET /api/clients → 200 OK** (lista de clientes)
4. ✅ **POST /api/clients → 201 Created** (cliente creado exitosamente)
5. ✅ **DELETE /api/clients/X → 204 No Content** (soft delete)
6. ✅ **GET /api/clients/X → 404** (cliente eliminado no se encuentra)
7. ✅ **403 Forbidden** (auditor intenta crear cliente)
8. ✅ **401 Unauthorized** (request sin token)
9. ✅ **jwt.io** (token decodificado mostrando claims)
10. ✅ **Filtrado**: GET /api/clients?email=maria@example.com
11. ✅ **Paginación**: GET /api/clients?pageNumber=1&pageSize=5
12. ✅ **POST /api/contacts** (crear contacto)

---

## Notas de Seguridad

- El token JWT **NO está encriptado**, solo firmado (cualquiera puede leerlo en jwt.io)
- **NUNCA** incluir información sensible en el token (contraseñas, tarjetas de crédito, etc.)
- El token expira en 60 minutos por seguridad
- La contraseña se valida con BCrypt (hash seguro)
- Usa HTTPS en producción (el token viaja en el header)

---

## Referencias

- **Documentación completa**: Ver `SWAGGER-TESTING-GUIDE.md`
- **Solución de problemas**: Ver `TROUBLESHOOTING.md`
- **Colección Postman**: Ver `postman/CRM-Collection.json`
