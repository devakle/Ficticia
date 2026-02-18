# Ficticia - Documento para Cliente

## 1. Que es Ficticia

Ficticia es una plataforma para operar el ciclo completo de gestion de personas, con:
- datos base de persona.
- atributos de negocio configurables sin rehacer el sistema.
- IA para normalizar condiciones y calcular riesgo.
- seguridad por roles para controlar quien ve y quien modifica.

## 2. Problemas que resuelve

- Informacion de personas dispersa o inconsistente.
- Alta dependencia de desarrollo para agregar nuevos campos.
- Procesos manuales lentos para analisis y evaluacion de riesgo.
- Riesgo operativo por falta de controles de acceso claros.

## 3. Solucion entregada

### Nucleo operativo
- Alta, actualizacion y activacion/inactivacion de personas.
- Busqueda combinando filtros fijos y dinamicos.

### Flexibilidad de negocio
- Catalogo de atributos configurable (tipo, reglas, estado, filtro).
- Carga de atributos por persona con validaciones automaticas.

### IA aplicada
- Normalizacion de texto libre a condicion estandar.
- Score de riesgo con banda y razones.

### Seguridad
- Login con token.
- Permisos por rol:
  - Viewer: solo lectura.
  - Manager: operacion de personas.
  - Admin: control total (incluye catalogos).

## 4. Casos de uso cubiertos

1. Registrar una nueva persona y consultarla.
2. Editar datos y cambiar estado activo/inactivo.
3. Definir nuevos atributos de negocio (ej. medico, administrativo, comercial).
4. Cargar atributos por persona con validacion de reglas.
5. Buscar personas por filtros avanzados (incluyendo atributos dinamicos).
6. Normalizar una condicion desde texto libre.
7. Calcular riesgo de una persona para apoyo a decision.

## 5. Valor para el negocio

- Menor tiempo para adaptar el sistema a nuevos requerimientos.
- Mejor calidad de datos por validaciones centralizadas.
- Mayor trazabilidad y control de acceso.
- Base escalable para automatizacion y analitica avanzada.

## 6. Confiabilidad de la solucion

- Pruebas automatizadas para auth, permisos, people, atributos e IA.
- Integracion continua que valida backend y frontend en cada cambio.
- Arranque robusto del sistema aun en escenarios de inicio concurrente.

## 7. Cambios destacados de esta version

- Se robustecio el arranque contra SQL Server (espera activa, retries y lock de migraciones).
- Se estandarizo el logging para trazabilidad de requests y casos CQRS.
- Se reforzo la inicializacion de catalogos, roles y usuarios demo por rol.
- Se consolidaron busqueda paginada y filtros dinamicos en UI.

## 8. Accesos demo para la presentacion

- Admin: `admin@ficticia.local` / `Admin123!`
- Manager: `manager@ficticia.local` / `Manager123!`
- Viewer: `viewer@ficticia.local` / `Viewer123!`
- La UI incluye botones de acceso rapido por rol y muestra el rol activo.
- Para demostrar seguridad desde el front:
  1. Entrar como Viewer y ejecutar una accion de escritura (debe devolver `403`).
  2. Entrar como Manager y mostrar operacion de personas.
  3. Entrar como Admin y mostrar gestion de definiciones.

## 9. Alcance cubierto hoy

### Incluido
- Operacion completa de personas.
- Atributos dinamicos con reglas.
- Seguridad por roles.
- Integracion IA para normalizacion y score.

### Siguiente etapa recomendada
1. Observabilidad avanzada y tableros operativos.
2. Auditoria de cambios y reportes de cumplimiento.
3. Flujo IA con fallback y metricas de calidad por entorno.
4. Endurecimiento de seguridad para produccion (secretos, hardening, compliance).

## 10. Demo ejecutiva sugerida (10-15 min)

1. Login.
2. Alta de persona.
3. Carga de atributos.
4. Busqueda con filtro dinamico.
5. Normalizacion IA.
6. Score de riesgo.
7. Cierre con roadmap.
