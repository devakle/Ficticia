# Demo Script - Ficticia

Guion de demo funcional y tecnico (15-20 minutos) alineado al estado actual del sistema.

## 1. Preparacion previa

### Infraestructura
1. Levantar SQL Server + Redis:
```bash
docker compose -f docker/docker-compose.yml up -d --wait --wait-timeout 180
```
2. Verificar backend arriba y Swagger disponible.
3. Levantar frontend Angular.

### Credenciales demo
- `admin@ficticia.local` / `Admin123!` (`Admin`)
- `manager@ficticia.local` / `Manager123!` (`Manager`)
- `viewer@ficticia.local` / `Viewer123!` (`Viewer`)
- En la UI hay accesos rapidos por rol en la seccion de conexion.

### Datos base esperados
- Definiciones seed disponibles:
  - `drives`
  - `uses_glasses`
  - `diabetic`
  - `disease_text`
  - `condition_code`

## 2. Agenda sugerida

1. Contexto del problema (2 min).
2. Flujo operativo principal (8-10 min).
3. Seguridad por roles (3 min).
4. IA aplicada (3-4 min).
5. Cierre y roadmap (2 min).

## 3. Flujo operativo principal

### Paso A - Login
- Iniciar sesion (UI o `POST /api/v1/auth/login`).
- Mostrar que la respuesta incluye token + roles.
- Mensaje clave: el acceso se controla por rol.

### Paso B - Crear persona
- Crear persona con documento unico.
- Confirmar alta exitosa.
- Consultar por ID para validar persistencia.

### Paso C - Gestion de atributos
- Cargar formulario de atributos de la persona.
- Asignar valores (ej. `condition_code=diabetes`, `diabetic=true`).
- Guardar y volver a consultar para verificar.

### Paso D - Busqueda dinamica
- Buscar por campos base (ej. documento).
- Buscar por dinamico: `attr.condition_code=diabetes`.
- Mostrar paginacion y total de resultados.

## 4. Seguridad por roles (mini-demostracion)

### Viewer
1. Ingresar desde UI con boton `Entrar como Viewer`.
2. Buscar personas: permitido.
3. Intentar crear persona o guardar atributos: debe responder `403`.
4. Intentar crear/editar definiciones: debe responder `403`.

### Manager
1. Ingresar con `Entrar como Manager`.
2. Crear/actualizar persona: permitido.
3. Intentar crear definicion de atributo: debe responder `403`.

### Admin
1. Ingresar con `Entrar como Admin`.
2. Acceso completo, incluida gestion de definiciones.

## 5. IA aplicada

### Normalizacion
1. Enviar texto libre (ej. "Paciente con diabetes tipo 2").
2. Llamar `POST /api/v1/ai/conditions/normalize`.
3. Mostrar:
- `code`
- `confidence`
- `matchedTerms`
- `suggestedAttributes`

## 6. Casos de error para mostrar robustez

1. Crear persona invalida -> `400 Validation error`.
2. Documento duplicado -> `409 people.duplicate_identification`.
3. Filtro dinamico invalido -> `400 filters.invalid`.

## 7. Mensajes clave de cierre

1. El modelo de datos es flexible por atributos dinamicos.
2. Seguridad y calidad estan integradas en el core.
3. La IA esta encapsulada y puede evolucionar sin romper el dominio.
4. Hay cobertura automatizada para los casos criticos de negocio.

## 8. Plan B si algo falla en vivo

1. Ejecutar demo en Swagger con ejemplos predefinidos.
2. Mostrar pruebas de integracion equivalentes al caso fallido.
3. Explicar arquitectura y observabilidad con logs estructurados.
4. Cerrar con roadmap y proximo sprint.
