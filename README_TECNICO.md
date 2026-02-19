# Ficticia - Documento Tecnico

Documento de referencia tecnica del estado actual del proyecto.

## 1. Objetivo tecnico

Ficticia implementa una arquitectura modular para gestionar personas y atributos dinamicos, con:
- API segura por JWT + RBAC.
- pipeline CQRS con trazabilidad y validacion.
- startup resiliente para SQL Server.
- capacidades de IA desacopladas del core de negocio.

## 2. Arquitectura y composicion

### 2.1 Solucion
```text
backend/src/Api.Host
backend/src/BuildingBlocks
backend/src/Modules/Modules.People
backend/src/Modules/Modules.Identity
backend/src/Modules/Modules.AI
backend/tests/*
frontend/*
```

### 2.2 Composicion en `Program.cs`
- Logging bootstrap con Serilog.
- Registro de modulos:
  - `AddBuildingBlocks()`
  - `AddPeopleModule(...)`
  - `AddAiModule(...)`
- Registro de Identity + JWT + policies.
- Registro MediatR y validators.
- Middlewares:
  - `RequestLoggingMiddleware`
  - `ExceptionMiddleware`
- Startup SQL y seed tolerante a concurrencia.

## 3. Flujo de arranque robusto SQL

Se aplico una estrategia defensiva para evitar fallos intermitentes de entorno local/CI:

1. `WaitUntilServerReadyAsync(...)`
- Conecta a `master`.
- Ejecuta `SELECT 1`.
- Reintentos con backoff.
- Detecta errores transitorios (`SqlException` comunes y timeouts).

2. `RunWithDatabaseLockAsync(...)`
- Usa `sp_getapplock` exclusivo por DB (`ficticia:migrate:{db}`).
- Evita colisiones de migracion/seeding cuando hay varios procesos.

3. `EnsureDatabaseExistsAsync(...)`
- Crea DB si falta con `CREATE DATABASE` idempotente.
- Tolera error `1801` (already exists).

4. `GetPendingMigrationsWithRetriesAsync(...)` + `MigrateWithRetriesAsync(...)`
- Evalua pendientes y migra solo si corresponde.
- Reintenta ante fallas transitorias.

5. Seed
- `SeedPeopleDefaults`:
  - atributos base (`drives`, `uses_glasses`, `diabetic`, `disease_text`, `condition_code`).
- `SeedIdentityDefaults`:
  - roles `Admin`, `Manager`, `Viewer`.
  - usuarios demo por rol (`Admin`, `Manager`, `Viewer`).

## 4. Pipeline de request y observabilidad

### 4.1 Middleware HTTP
- `RequestLoggingMiddleware`
  - log de inicio/fin/fallo HTTP.
  - incluye metodo, path, host, esquema, status, elapsed, `TraceId`.
  - severidad por status code.

- `ExceptionMiddleware`
  - `ValidationException` -> `400` + `ProblemDetails`.
  - violacion de unique constraint -> `409` + `ProblemDetails`.
  - error no controlado -> `500`.

### 4.2 Pipeline MediatR
- `LoggingBehavior<TRequest,TResponse>`
  - START/OK/FAIL por request.
  - duracion en ms.
  - payload/response estructurados.
- `ValidationBehavior<TRequest,TResponse>`
  - ejecuta `IValidator<TRequest>`.
  - log warning con detalle de errores.
  - aborta flujo con `ValidationException`.

### 4.3 Formato de logs
- Serilog consola con `outputTemplate`:
  - timestamp
  - nivel
  - `Component`
  - `TraceId`
  - `SourceContext`
  - mensaje + excepcion

## 5. Seguridad

### 5.1 Auth
- Endpoint: `POST /api/v1/auth/login`.
- Emite JWT con claims de identidad y roles.

### 5.2 Authorization policies
- `People.Read`: Admin/Manager/Viewer.
- `People.Write`: Admin/Manager.
- `Attributes.Manage`: Admin.

Nota de estado actual:
- `POST /api/v1/people` exige `People.Write`.
- `PUT/PATCH /api/v1/people` heredan `People.Read` y deben endurecerse en siguiente iteracion.

### 5.3 Contratos de acceso validados por tests
- Viewer:
  - puede leer personas.
  - no puede escribir personas.
  - no puede gestionar definiciones.
- Manager:
  - puede escribir personas.
  - no puede gestionar definiciones.
- Admin:
  - acceso completo.

## 6. Dominio People y atributos dinamicos

### 6.1 AttributeDataType
- `1` Boolean
- `2` String
- `3` Number
- `4` Date
- `5` Enum

### 6.2 Validacion de shape
- Se permite enviar 0 valores (clear) o 1 valor.
- Si hay >1 valor -> `attributes.invalid_shape`.
- Si el tipo no coincide -> `attributes.invalid_shape`.

### 6.3 Validacion de reglas
`AttributeValidationRules` soporta:
- `required`
- `allowedValues` (enum)
- `maxLength`, `regex` (string)
- `min`, `max` (number)
- `minDate`, `maxDate` (date)

### 6.4 Busqueda dinamica
Admite dos formatos de query:
- `attr.key=value`
- `attr[key]=value`

Validaciones:
- key inexistente/no filtrable -> `filters.invalid`.
- tipo invalido (ej. bool mal parseado) -> `filters.invalid`.

Paginacion:
- `page >= 1`
- `pageSize` clamped `1..100`
- respuesta `PagedResult<T>` con `items/total/page/pageSize`.

## 7. Modulo AI

### 7.1 Normalizacion
- Handler: `NormalizeConditionHandler`.
- Servicio: `OpenAiConditionNormalizer`.
- Flujo:
  - obtiene codigos permitidos desde catalogo People.
  - llama OpenAI Responses con JSON schema estricto.
  - normaliza code en lowercase.
  - si confianza >= threshold, sugiere `condition_code`.

### 7.2 Errores AI
- `ai.invalid_input`
- `ai.provider_failed`

### 7.3 Observacion tecnica
- Existe implementacion de `DictionaryFallback(...)` en normalizador, pero actualmente no se invoca en la ruta principal.

## 8. Configuracion por ambiente

### 8.1 Claves backend
- Connection strings:
  - `ConnectionStrings:PeopleDb`
  - `ConnectionStrings:IdentityDb`
- JWT:
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiresMinutes`
- OpenAI:
  - `OpenAI:ApiKey`, `OpenAI:BaseUrl`, `OpenAI:Model`, `OpenAI:ConfidenceThreshold`
- Redis opcional:
  - `Redis:ConnectionString`

### 8.2 Nota de consistencia
- La opcion consumida por codigo es `OpenAI:ConfidenceThreshold`.

## 9. Frontend tecnico

### 9.1 Caracteristicas implementadas
- Login y persistencia de token en `localStorage`.
- CRUD de personas y cambio de estado.
- Catalogo de definiciones y edicion de reglas.
- Edicion de atributos por persona.
- Filtros dinamicos tipados y paginacion visual (incluye ellipsis).
- Normalizacion IA sobre persona seleccionada.

### 9.2 Integracion API
- Servicios por dominio:
  - `AuthApiService`
  - `PeopleApiService`
  - `AttributesApiService`
  - `AiApiService`

## 10. Testing

### 10.1 Unit
- `AttributeRulesValidatorTests`
- `AttributeValueShapeValidatorTests`

### 10.2 Integration
- `AuthLoginTests`
- `RoleAuthorizationTests`
- `PeopleEndpointsTests`
- `PeopleAttributeUpsertTests`
- `AttributeDefinitionsTests`
- `AiEndpointsTests`

### 10.3 Infra de tests
- SQL Server con Testcontainers.
- Override de `IDistributedCache` a memory cache para determinismo.
- AI sustituida por fakes en tests de endpoints IA.

## 11. CI/CD

Workflow: `.github/workflows/ci.yml`

Backend:
- SQL Server service en job.
- restore/build/test release.
- export de `.trx`.

Frontend:
- install/test/build.
- `lint` condicionado con `--if-present`.

## 12. Operacion y troubleshooting

### SQL no disponible al arrancar
- validar `docker compose ps`.
- revisar healthcheck de SQL container.
- verificar password/connection string.

### Fallos de auth
- revisar `Jwt:Key` no vacio.
- validar clock del host (expiracion token).

### Fallos AI
- revisar `OpenAI:ApiKey`, `OpenAI:BaseUrl`, `OpenAI:Model`.
- verificar reachability de internet/salida HTTPS del entorno.

## 13. Backlog tecnico recomendado
1. Activar health checks (`/health/live`, `/health/ready`).
2. Completar wiring de fallback IA controlado por config.
3. Integrar OpenTelemetry (logs/traces/metrics).
4. Agregar contract tests de API y e2e de frontend.
5. Externalizar secretos (Vault/Secret Manager).
