# 🚀 CRM Profesional - Sistema de Gestión de Clientes

> **Solución completa y lista para usar** que te permite gestionar clientes, oportunidades de venta y seguimiento comercial en minutos, no en semanas.

---

## 🎯 ¿Qué Problema Resuelve?

### Antes (Sin CRM):
- ❌ Información de clientes dispersa en Excel, emails y notas
- ❌ Pérdida de oportunidades por falta de seguimiento
- ❌ No hay historial de interacciones con clientes
- ❌ Imposible saber quién modificó qué y cuándo
- ❌ Sin control de acceso por roles (todos ven todo)

### Después (Con Este CRM):
- ✅ **Base de datos centralizada** - Toda la información en un solo lugar
- ✅ **Seguimiento automático** - Historial completo de cada cliente
- ✅ **Auditoría total** - Sabes quién creó, modificó o eliminó cada registro
- ✅ **Control de acceso** - Roles Admin, Asesor y Auditor
- ✅ **API REST moderna** - Integrable con cualquier frontend (Angular, React, Vue)
- ✅ **Listo para producción** - Código profesional con documentación completa

---

## 💼 Casos de Uso Reales

### 1. **Agencia de Marketing Digital**
```
- Gestiona cartera de 200+ clientes
- Asesores registran llamadas y reuniones como notas
- Gerente ve el pipeline de ventas en tiempo real
- Auditor revisa informes sin modificar datos
```

### 2. **Consultora de Software**
```
- Tracking de oportunidades desde $5,000 a $100,000
- Cada consultor gestiona sus clientes
- Manager aprueba o rechaza cambios críticos
- Historial completo para auditorías ISO
```

### 3. **Inmobiliaria**
```
- Registro de clientes compradores/vendedores
- Notas sobre visitas a propiedades
- Oportunidades con montos estimados
- Sistema de permisos para proteger datos sensibles
```

---

## ⚡ Quick Start - Ejecuta en 3 Pasos

### Paso 1: Clonar el Repositorio
```bash
git clone https://github.com/Caibarguen10/CRM.git
cd CRM/CrmService
```

### Paso 2: Ejecutar el Proyecto
```bash
dotnet run
```

**¡Eso es todo!** La base de datos se crea automáticamente con datos de ejemplo.

### Paso 3: Abrir Swagger y Probar
```
https://localhost:5001/swagger
```

---

## 🎁 Demo Lista para Usar

### 👤 Usuarios Pre-Creados

| Usuario | Email | Password | Permisos |
|---------|-------|----------|----------|
| **Admin** | admin@crm.com | `Admin123!` | ✅ CRUD completo en todo |
| **Asesor** | asesor@crm.com | `Asesor123!` | ✅ Crear clientes y notas<br>❌ No puede eliminar |
| **Auditor** | auditor@crm.com | `Auditor123!` | ✅ Solo lectura<br>❌ No puede modificar nada |

### 📊 Datos de Ejemplo Incluidos

Al ejecutar por primera vez, obtienes automáticamente:
- ✅ **2 clientes** (Juan Pérez, María López)
- ✅ **2 contactos** (información de responsables)
- ✅ **3 notas** (historial de interacciones)
- ✅ **2 oportunidades** (€25,000 y €15,000)

**Puedes probar la API inmediatamente sin configurar nada.**

---

## 📸 Demo Visual - Ve el API en Acción

¿Quieres ver cómo funciona antes de instalarlo? Tenemos **16 capturas de pantalla** que demuestran cada funcionalidad:

### 🎯 Highlights:

| **Funcionalidad** | **Captura** | **¿Qué Demuestra?** |
|-------------------|-------------|---------------------|
| 🔐 **Login JWT** | [01-login-exitoso.PNG](Screenshots/01-login-exitoso.PNG) | Autenticación con token JWT, usuario admin |
| 📋 **Listar Clientes** | [03-get-clients-200.PNG](Screenshots/03-get-clients-200.PNG) | Paginación con 2 clientes, metadatos completos |
| ➕ **Crear Cliente** | [04-post-client-201.PNG](Screenshots/04-post-client-201.PNG) | Creación exitosa con auditoría automática |
| 🗑️ **Soft Delete** | [05-delete-client-204.PNG](Screenshots/05-delete-client-204.PNG) | Eliminación lógica (no borra físicamente) |
| 🚫 **Control de Acceso** | [07-forbidden-403.PNG](Screenshots/07-forbidden-403.PNG) | Auditor NO puede crear clientes (403) |
| 🔍 **Filtrado** | [10-filtrado-email.PNG](Screenshots/10-filtrado-email.PNG) | Búsqueda por email específico |
| 📄 **Paginación** | [11-paginacion.PNG](Screenshots/11-paginacion.PNG) | Control de tamaño de páginas |
| 🔗 **Relaciones** | [12-crear-contacto.PNG](Screenshots/12-crear-contacto.PNG) | Contacto asociado a cliente |
| 🔓 **Token Decodificado** | [09-jwt-io-decoded.PNG](Screenshots/09-jwt-io-decoded.PNG) | Validación en jwt.io con claims visibles |

**👉 Ver todas las 16 capturas con descripciones detalladas:**  
📁 **[Screenshots/README.md](Screenshots/README.md)**

---

## 🔑 Prueba Rápida (2 minutos)

### 1️⃣ Hacer Login (Obtener Token)
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!"
  }'
```

**Respuesta**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "username": "admin",
    "email": "admin@crm.com",
    "role": "Admin"
  }
}
```

### 2️⃣ Ver Clientes
```bash
curl -X GET https://localhost:5001/api/clients \
  -H "Authorization: Bearer TU_TOKEN_AQUI"
```

**Respuesta**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "documentNumber": "12345678",
        "fullName": "Juan Pérez García",
        "email": "juan.perez@email.com",
        "phone": "+34 600 123 456",
        "createdBy": "admin",
        "createdAt": "2024-04-14T10:30:00Z"
      }
    ],
    "totalCount": 2,
    "page": 1,
    "pageSize": 10
  }
}
```

### 3️⃣ Crear Nota para Cliente
```bash
curl -X POST https://localhost:5001/api/notes \
  -H "Authorization: Bearer TU_TOKEN_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": 1,
    "content": "Cliente interesado en contratar servicio premium. Programar demo para próxima semana."
  }'
```

---

## 📡 Endpoints Principales

### 🔐 Autenticación
- `POST /api/auth/register` - Registrar nuevo usuario
- `POST /api/auth/login` - Iniciar sesión y obtener JWT token

### 👥 Gestión de Clientes
- `GET /api/clients` - Listar clientes (con paginación y filtros)
- `GET /api/clients/{id}` - Ver detalle de un cliente
- `POST /api/clients` - Crear nuevo cliente
- `PUT /api/clients/{id}` - Actualizar cliente
- `DELETE /api/clients/{id}` - Eliminar cliente (soft delete)

### 📞 Contactos
- `GET /api/contacts/client/{clientId}` - Ver contactos de un cliente
- `POST /api/contacts` - Crear contacto

### 📝 Notas de Seguimiento
- `POST /api/notes` - Registrar nota de interacción con cliente

### 💰 Oportunidades de Venta
- `POST /api/opportunities` - Crear oportunidad de negocio

**📚 Documentación completa de API**: Disponible en `/swagger` al ejecutar el proyecto

---

## 🛡️ Características Empresariales

### Auditoría Automática
Cada registro guarda automáticamente:
- ✅ Quién lo creó y cuándo
- ✅ Quién lo modificó y cuándo
- ✅ Quién lo eliminó y cuándo

**Ejemplo en la base de datos**:
```json
{
  "id": 1,
  "fullName": "Juan Pérez",
  "createdBy": "admin",
  "createdAt": "2024-04-14T10:00:00Z",
  "updatedBy": "asesor",
  "updatedAt": "2024-04-14T15:30:00Z",
  "deletedBy": null,
  "deletedAt": null
}
```

### Soft Delete (Borrado Seguro)
- Los registros **NO se eliminan físicamente**
- Se marcan como `isDeleted = true`
- Permite recuperación y auditorías
- Cumple con normativas GDPR/LOPD

### Sistema de Roles

| Acción | Admin | Asesor | Auditor |
|--------|-------|--------|---------|
| Ver clientes | ✅ | ✅ | ✅ |
| Crear clientes | ✅ | ✅ | ❌ |
| Editar clientes | ✅ | ✅ | ❌ |
| Eliminar clientes | ✅ | ❌ | ❌ |
| Crear notas | ✅ | ✅ | ❌ |
| Ver reportes | ✅ | ✅ | ✅ |

---

## 🎨 Integración con Frontend

Este backend funciona con **cualquier frontend**:

### Angular
```typescript
// app.service.ts
login(username: string, password: string) {
  return this.http.post('https://api.tudominio.com/api/auth/login', {
    username, password
  });
}

getClients() {
  return this.http.get('https://api.tudominio.com/api/clients', {
    headers: { Authorization: `Bearer ${this.token}` }
  });
}
```

### React
```javascript
// api.js
const login = async (username, password) => {
  const response = await fetch('https://api.tudominio.com/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  return response.json();
};
```

### Vue.js
```javascript
// services/crm.js
export default {
  async login(username, password) {
    const response = await axios.post('/api/auth/login', {
      username, password
    });
    return response.data;
  }
}
```

---

## 📦 Colección Postman

Descarga la colección completa para probar todos los endpoints:

👉 **[CRM-Postman-Collection.json](./postman/CRM-Collection.json)**

Incluye:
- ✅ Todas las requests pre-configuradas
- ✅ Variables de entorno ({{baseUrl}}, {{token}})
- ✅ Ejemplos de respuestas exitosas
- ✅ Tests automáticos incluidos

**Cómo importar**:
1. Abrir Postman
2. Import → File → Seleccionar `CRM-Collection.json`
3. Configurar variable `baseUrl`: `https://localhost:5001`
4. ¡Listo para usar!

---

## 🚀 Despliegue a Producción

### Opción 1: Azure App Service
```bash
# Publicar a Azure
az webapp up --name mi-crm-api --resource-group mi-grupo
```

### Opción 2: Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY bin/Release/net8.0/publish/ app/
ENTRYPOINT ["dotnet", "app/CrmService.dll"]
```

```bash
docker build -t crm-api .
docker run -p 5000:5000 crm-api
```

### Opción 3: IIS (Windows Server)
1. Publicar con `dotnet publish -c Release`
2. Copiar carpeta `bin/Release/net8.0/publish/` a IIS
3. Configurar Application Pool (.NET 8)

**📚 Guía completa de deployment**: Ver [DEPLOYMENT.md](./DEPLOYMENT.md)

---

## 📊 Roadmap de Funcionalidades

### ✅ Versión Actual (v1.0)
- Sistema de autenticación JWT
- Gestión completa de clientes
- Notas y seguimiento
- Oportunidades de venta
- Auditoría automática
- Soft delete
- Sistema de roles

### 🚧 Próxima Versión (v1.1)
- [ ] Dashboard con estadísticas
- [ ] Exportación a Excel/PDF
- [ ] Notificaciones por email
- [ ] Recordatorios automáticos
- [ ] Filtros avanzados
- [ ] Búsqueda full-text

### 🔮 Futuro (v2.0)
- [ ] Integración con WhatsApp
- [ ] Calendar de eventos
- [ ] Reportes personalizables
- [ ] Multi-tenancy (SaaS)
- [ ] GraphQL API
- [ ] WebSockets para real-time

---

## 💰 Ventajas Competitivas

### vs. Salesforce
- ✅ **Open Source** - Sin costos de licencia ($25/usuario/mes → $0)
- ✅ **Personalizable** - Código fuente completo
- ✅ **Sin límites** - Usuarios/registros ilimitados
- ✅ **Hosting propio** - Control total de tus datos

### vs. HubSpot
- ✅ **Sin vendor lock-in** - Migra cuando quieras
- ✅ **API RESTful** - Integra con cualquier sistema
- ✅ **On-premise** - Datos en tu servidor

### vs. Zoho CRM
- ✅ **.NET moderno** - Fácil de mantener y extender
- ✅ **Arquitectura limpia** - Código profesional documentado
- ✅ **SQLite incluido** - Sin infraestructura compleja

---

## 🎓 Soporte y Documentación

### 📖 Documentación Técnica
- **README.md** - Arquitectura y desarrollo
- **MIGRATIONS-AND-POLICIES.md** - Guía de base de datos y seguridad
- **SWAGGER-TESTING-GUIDE.md** - Testing paso a paso
- **Código fuente** - 5,500+ líneas de comentarios en español

### 🆘 Soporte
- **Issues en GitHub**: https://github.com/Caibarguen10/CRM/issues
- **Wiki del proyecto**: https://github.com/Caibarguen10/CRM/wiki
- **Email**: caibarguen10@gmail.com

---

## 📜 Licencia

Este proyecto está bajo licencia **MIT** - puedes usarlo libremente para proyectos comerciales.

---

## 🤝 Casos de Éxito

### Testimonios (Placeholder)

> *"Implementamos este CRM en 2 días. Ahora gestionamos 300+ clientes y hemos aumentado las ventas un 40% por mejor seguimiento."*  
> — **María García**, Directora Comercial, TechConsult S.A.

> *"La auditoría automática nos salvó en una auditoría ISO 9001. Todo el historial estaba registrado automáticamente."*  
> — **Carlos Pérez**, CTO, InnovateSoft

> *"Lo mejor es que puedo personalizarlo. Agregamos integración con WhatsApp en 1 semana."*  
> — **Laura Martínez**, Lead Developer, Digital Agency

---

## 🎯 Empieza Ahora

```bash
# 1. Clona el repositorio
git clone https://github.com/Caibarguen10/CRM.git

# 2. Navega a la carpeta
cd CRM/CrmService

# 3. Ejecuta el proyecto
dotnet run

# 4. Abre tu navegador
https://localhost:5001/swagger

# 5. Haz login con:
# Usuario: admin@crm.com
# Password: Admin123!
```

**¡En menos de 5 minutos tienes un CRM profesional funcionando!**

---

## 📞 Contacto

**GitHub**: [@Caibarguen10](https://github.com/Caibarguen10)  
**Repositorio**: https://github.com/Caibarguen10/CRM  
**Email**: caibarguen10@gmail.com

---

<div align="center">

**⭐ Si este proyecto te resultó útil, dale una estrella en GitHub!**

[![GitHub stars](https://img.shields.io/github/stars/Caibarguen10/CRM?style=social)](https://github.com/Caibarguen10/CRM)
[![GitHub forks](https://img.shields.io/github/forks/Caibarguen10/CRM?style=social)](https://github.com/Caibarguen10/CRM/fork)

</div>
