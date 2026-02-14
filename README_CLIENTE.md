# Ficticia - Documento para Cliente

## 1. Que es Ficticia
Ficticia es una plataforma para gestionar personas, sus atributos de negocio y capacidades de IA para estandarizar informacion y estimar riesgo.

## 2. Problema que resuelve
- Datos dispersos e inconsistentes de personas.
- Dificultad para agregar nuevos campos sin cambios costosos.
- Decisiones lentas por falta de estandarizacion.

## 3. Solucion propuesta
- Gestion centralizada de personas.
- Atributos dinamicos configurables (sin rediseñar todo el sistema).
- IA para:
- normalizar condiciones desde texto libre.
- calcular score de riesgo con razones.
- Seguridad por roles (Admin, Manager, Viewer).

## 4. Beneficios para el negocio
- Menor time-to-market para nuevos requerimientos.
- Mejor calidad de dato.
- Mayor velocidad de evaluacion.
- Escalabilidad funcional sin rehacer la plataforma.

## 5. Modulos funcionales
- Personas: alta, edicion, estado, busqueda.
- Atributos: definicion de catalogo y carga de valores.
- IA: normalizacion de condiciones y scoring.
- Seguridad: login, JWT y permisos por rol.

## 6. Casos de uso claves
1. Alta de persona y gestion de su informacion base.
2. Carga de atributos medicos y administrativos.
3. Busqueda avanzada por filtros dinamicos.
4. Estandarizacion de condiciones medicas desde texto.
5. Score de riesgo para apoyo a decisiones.

## 7. Seguridad y gobierno
- Acceso autenticado con token.
- Permisos por rol:
- Viewer: consulta.
- Manager: consulta y operacion sobre personas.
- Admin: administracion completa (incluye catalogos).

## 8. Calidad y confiabilidad
- Pruebas unitarias e integracion automatizadas.
- Pipeline de CI para validar backend y frontend.
- Validaciones de negocio en API para evitar datos invalidos.

## 9. Roadmap ejecutivo
### Fase 1
- Endurecimiento productivo, observabilidad y monitoreo.
### Fase 2
- Auditoria, permisos mas granulares y mas filtros avanzados.
### Fase 3
- IA con medicion continua de calidad y mejoras de precision.
### Fase 4
- Compliance y gobierno de datos sensibles.

## 10. Propuesta de presentacion al cliente
1. Contexto del problema.
2. Demo funcional de flujo principal.
3. Seguridad y control de acceso.
4. Roadmap y tiempos estimados.
5. Proximos pasos y alcance de la siguiente fase.

