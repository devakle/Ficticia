# Demo Script - Ficticia

Guion operativo para demo de 15-20 minutos (cliente + tecnico).

## 1. Preparacion previa
1. Backend levantado.
2. Frontend levantado.
3. SQL Server operativo.
4. Usuario admin disponible:
- `admin@ficticia.local`
- `Admin123!`

## 2. Agenda sugerida
1. Introduccion (2 min).
2. Flujo funcional principal (8-10 min).
3. Seguridad por roles (3-4 min).
4. IA aplicada (3-4 min).
5. Cierre y roadmap (2 min).

## 3. Flujo funcional principal

### Paso A - Login
- Ejecutar login por API o UI.
- Mostrar token y roles en respuesta.
- Mensaje clave: acceso controlado por rol.

### Paso B - Alta de persona
- Crear una persona nueva.
- Confirmar respuesta `200`.
- Mostrar consulta por ID.

### Paso C - Atributos dinamicos
- Consultar formulario de atributos de la persona.
- Cargar atributos (por ejemplo `condition_code`).
- Mostrar validacion si el valor no esta permitido.

### Paso D - Busqueda dinamica
- Buscar por filtros estaticos.
- Buscar por filtro dinamico `attr.condition_code=diabetes`.
- Mostrar que retorna la persona esperada.

## 4. Seguridad por roles

### Viewer
- Puede consultar personas.
- No puede crear personas (`403`).
- No puede gestionar definiciones (`403`).

### Manager
- Puede crear/editar personas.
- No puede gestionar definiciones (`403`).

### Admin
- Acceso completo.

## 5. IA aplicada

### Normalizacion
- Enviar texto libre a `/api/v1/ai/conditions/normalize`.
- Mostrar salida normalizada.

### Risk score
- Ejecutar `/api/v1/ai/people/{id}/risk-score`.
- Mostrar score, banda y razones.

## 6. Mensajes clave para cierre
1. Plataforma modular y escalable.
2. Seguridad y calidad integradas.
3. IA desacoplada para evolucion controlada.
4. Roadmap claro y fases de crecimiento.

## 7. Plan B (si algo falla en vivo)
1. Usar ejemplos pre-cargados.
2. Mostrar pruebas automatizadas ejecutadas.
3. Mostrar endpoints por Swagger con respuestas esperadas.
4. Continuar con arquitectura y roadmap.

