# Template para agregar al README.md después del deploy

Agregar esta sección después del "Quick Start" (alrededor de línea 75):

```markdown
---

## 🌐 Demo en Vivo

**✨ Prueba el API sin instalar nada:**

🔗 **URL:** https://[TU-URL-AQUI].up.railway.app  
📖 **Swagger UI:** https://[TU-URL-AQUI].up.railway.app/swagger  
🔐 **Estado:** ![Deploy](https://img.shields.io/badge/deploy-online-success)

### 🎮 Prueba Rápida:

**1. Login (obtener token):**
```bash
curl -X POST https://[TU-URL-AQUI].up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'
```

**2. Probar en Swagger:**
1. Abre: https://[TU-URL-AQUI].up.railway.app/swagger
2. Haz clic en "Authorize" 🔒
3. Usa las credenciales:
   - **Admin**: `admin` / `Admin123!`
   - **Asesor**: `asesor` / `Asesor123!`
   - **Auditor**: `auditor` / `Auditor123!`

**3. Ver clientes:**
```bash
curl https://[TU-URL-AQUI].up.railway.app/api/clients \
  -H "Authorization: Bearer [TU-TOKEN]"
```

---
```

## Instrucciones para ti:

1. Después de hacer el deploy, copia tu URL real
2. Reemplaza `[TU-URL-AQUI]` con tu URL
3. Agrega esta sección al README.md
4. Commit y push

Ejemplo de URL real:
- Railway: `crm-api-production-abc123.up.railway.app`
- Render: `crm-api-demo.onrender.com`
