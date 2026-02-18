# Frontend Ficticia (Angular)

Aplicacion de administracion para operar personas, atributos dinamicos y flujo IA sobre la API de Ficticia.

## 1. Requisitos

- Node `>= 20.19.0`
- npm `11.x`
- Backend `Api.Host` ejecutandose y accesible desde navegador.

## 2. Ejecutar en local

```bash
npm ci
npm start
```

La app queda disponible en `http://localhost:4200`.

## 3. Configuracion en runtime

La URL base de la API se carga desde la UI (campo `apiBaseUrl`).

Valores tipicos:
- `http://localhost:5000`
- `https://localhost:5001`

Credenciales de desarrollo por defecto:
- `admin@ficticia.local` / `Admin123!` (`Admin`)
- `manager@ficticia.local` / `Manager123!` (`Manager`)
- `viewer@ficticia.local` / `Viewer123!` (`Viewer`)

## 4. Funcionalidades principales

### 4.1 Autenticacion
- Login contra `POST /api/v1/auth/login`.
- Selector rapido de usuarios demo por rol (Admin/Manager/Viewer).
- Token Bearer almacenado en `localStorage` (`auth_token`) y roles en `auth_roles`.

### 4.2 Personas
- Buscar personas con filtros base y dinamicos.
- Paginacion de resultados (`page`, `pageSize`).
- Crear, actualizar y cambiar estado activo/inactivo.

### 4.3 Atributos dinamicos
- Listar, crear y actualizar definiciones.
- Editar reglas de validacion de cada definicion.
- Cargar/guardar atributos para la persona seleccionada.

### 4.4 IA
- Normalizar condicion medica desde texto libre.
- Aplicar atributos sugeridos por la normalizacion.
- Calcular score de riesgo de la persona seleccionada.

## 5. Servicios por dominio

- `src/app/core/services/auth-api.service.ts`
- `src/app/features/people/services/people-api.service.ts`
- `src/app/features/attributes/services/attributes-api.service.ts`
- `src/app/features/ai/services/ai-api.service.ts`

## 6. Scripts

- `npm start`: `ng serve`
- `npm run build`: build produccion
- `npm test`: tests unitarios (`ng test`)

## 7. Notas de integracion

- La app asume que backend ya esta autenticando por JWT y aplicando RBAC.
- Los filtros dinamicos se envian en formato `attr.<key>=<value>`.
- El frontend normaliza datos de paginacion para tolerar variantes de casing en respuesta (`items`/`Items`, etc.).
