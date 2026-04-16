# 🚀 Guía de Deploy - CRM API

Esta guía te muestra cómo hacer deploy del API CRM en plataformas gratuitas de hosting.

---

## 🎯 Opción 1: Railway.app (RECOMENDADO)

Railway es perfecto para demos: deploy automático, siempre activo, fácil configuración.

### ✅ Ventajas:
- Deploy automático desde GitHub
- $5 de crédito mensual gratis (suficiente para demo 24/7)
- HTTPS automático
- URL pública permanente

### 📋 Pasos:

#### 1. Crear cuenta en Railway
1. Ve a: https://railway.app/
2. Click en "Start a New Project"
3. Conecta tu cuenta de GitHub

#### 2. Deploy desde GitHub
1. Click en "Deploy from GitHub repo"
2. Selecciona: `Caibarguen10/CRM`
3. Railway detectará automáticamente que es .NET 8
4. Click en "Deploy Now"

#### 3. Configurar variables de entorno (Opcional)
Si quieres personalizar el JWT:
```
Jwt__SecretKey=TuClaveSecretaSuperSeguraAqui123456789!
Jwt__Issuer=CrmServiceAPI
Jwt__Audience=CrmServiceClient
```

#### 4. Obtener URL pública
1. Ve a tu proyecto en Railway
2. Click en "Settings" → "Networking"
3. Click en "Generate Domain"
4. Tu API estará en: `https://tu-proyecto.up.railway.app`

#### 5. Probar tu API
```bash
# Login
curl -X POST https://tu-proyecto.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'

# Swagger UI
https://tu-proyecto.up.railway.app/swagger
```

---

## 🎯 Opción 2: Render.com (100% GRATIS)

Render es completamente gratis, pero se "duerme" después de 15 min sin uso.

### ✅ Ventajas:
- 100% gratis (sin tarjeta)
- Deploy desde GitHub
- HTTPS automático

### ⚠️ Limitaciones:
- Se duerme tras 15 min de inactividad
- Primera request tarda ~30 segundos en despertar

### 📋 Pasos:

#### 1. Crear cuenta en Render
1. Ve a: https://render.com/
2. Registrarte con GitHub

#### 2. Crear Web Service
1. Click en "New +" → "Web Service"
2. Conecta tu repositorio: `Caibarguen10/CRM`
3. Configuración:
   - **Name:** `crm-api-demo`
   - **Root Directory:** `CrmService`
   - **Environment:** `Docker`
   - **Plan:** `Free`
4. Click en "Create Web Service"

#### 3. Configurar variables de entorno (Automático)
Render detecta automáticamente el puerto desde el Dockerfile.

#### 4. Obtener URL
Tu API estará en: `https://crm-api-demo.onrender.com`

#### 5. Probar
```bash
# Swagger (puede tardar ~30s la primera vez)
https://crm-api-demo.onrender.com/swagger
```

---

## 🎯 Opción 3: Azure App Service (MÁS PROFESIONAL)

Azure es ideal si quieres algo más "enterprise" para tu CV.

### ✅ Ventajas:
- $200 de crédito gratis primer mes
- Muy profesional para CV
- Escalable
- Integración con Azure DevOps

### ⚠️ Requisitos:
- Cuenta de Azure (requiere tarjeta)
- Azure CLI instalado

### 📋 Pasos:

#### 1. Instalar Azure CLI
```bash
# Windows
winget install Microsoft.AzureCLI

# macOS
brew install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

#### 2. Login en Azure
```bash
az login
```

#### 3. Crear App Service
```bash
# Crear Resource Group
az group create --name crm-rg --location eastus

# Crear App Service Plan (gratis)
az appservice plan create \
  --name crm-plan \
  --resource-group crm-rg \
  --sku F1 \
  --is-linux

# Crear Web App
az webapp create \
  --name crm-api-tu-nombre \
  --resource-group crm-rg \
  --plan crm-plan \
  --runtime "DOTNETCORE:8.0"

# Configurar deploy desde GitHub
az webapp deployment source config \
  --name crm-api-tu-nombre \
  --resource-group crm-rg \
  --repo-url https://github.com/Caibarguen10/CRM \
  --branch main \
  --manual-integration
```

#### 4. Tu API estará en:
```
https://crm-api-tu-nombre.azurewebsites.net
```

---

## 📊 Comparación

| Característica | Railway | Render | Azure |
|---------------|---------|--------|-------|
| **Precio** | $5/mes gratis | 100% gratis | $200 crédito |
| **Siempre activo** | ✅ Sí | ❌ Se duerme | ✅ Sí |
| **Setup** | ⚡ 2 min | ⚡ 5 min | ⏱️ 15 min |
| **Requiere tarjeta** | ⚠️ Sí (no cobra) | ✅ No | ⚠️ Sí |
| **Profesionalismo** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **URL personalizada** | ✅ Sí | ✅ Sí | ✅ Sí |

---

## 🎯 MI RECOMENDACIÓN

### Para Portfolio/Demo:
**Railway** - Deploy en 2 minutos, siempre activo, perfecto para mostrar en entrevistas.

### Para Aprendizaje:
**Render** - 100% gratis, bueno para practicar.

### Para CV Profesional:
**Azure** - Mencionar "Azure App Service" en CV se ve muy bien.

---

## 🔧 Troubleshooting

### Railway: "Build Failed"
**Problema:** El build falla al publicar.  
**Solución:** Verifica que el archivo `railway.json` esté en la raíz del proyecto.

### Render: "Application Failed to Start"
**Problema:** El puerto no está configurado correctamente.  
**Solución:** Verifica que `Program.cs` use la variable `PORT`:
```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
```

### Azure: "Deployment Failed"
**Problema:** Error al conectar con GitHub.  
**Solución:** Usa GitHub Actions en vez de deploy directo. Crea `.github/workflows/azure-deploy.yml`.

### Error: "Database not found"
**Problema:** SQLite no persiste en deploy (se borra al reiniciar).  
**Solución esperada:** La app crea la BD automáticamente con `EnsureCreated()` y hace seeding.

---

## 📝 Post-Deploy Checklist

Después de hacer deploy, verifica:

- [ ] Swagger UI accesible: `https://tu-url/swagger`
- [ ] Login funciona:
  ```bash
  curl -X POST https://tu-url/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"Admin123!"}'
  ```
- [ ] GET /api/clients retorna 401 sin token
- [ ] GET /api/clients retorna 200 con token
- [ ] Usuarios de demo creados automáticamente (admin, asesor, auditor)

---

## 🎉 Actualizar README con URL Live

Una vez deployado, actualiza tu `README.md`:

```markdown
## 🌐 Demo en Vivo

**URL:** https://tu-proyecto.up.railway.app  
**Swagger:** https://tu-proyecto.up.railway.app/swagger

**Credenciales de prueba:**
- Username: `admin` / Password: `Admin123!`
- Username: `asesor` / Password: `Asesor123!`
- Username: `auditor` / Password: `Auditor123!`
```

---

## 📞 Soporte

Si tienes problemas con el deploy:
1. **GitHub Issues**: https://github.com/Caibarguen10/CRM/issues
2. **Railway Docs**: https://docs.railway.app/
3. **Render Docs**: https://render.com/docs
4. **Azure Docs**: https://learn.microsoft.com/azure/app-service/

---

**¡Tu API estará online y lista para demos en menos de 5 minutos! 🚀**
