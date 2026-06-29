# Chat Roadmap

## Alcance

Mantener el roadmap maestro del modulo Ishikawa RCA a partir del estado real
del repositorio, no solo de ideas pendientes.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/API_CONTRACTS.md`
- `docs/AI_INTEGRATION.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/ROADMAP.md`

Luego revisar el proyecto real:

- Estructura `src`.
- Estructura `tests`.
- Controllers/API.
- Entidades de dominio.
- Servicios de aplicacion/infraestructura.
- Migraciones.
- Documentacion viva en `docs`.

## Estado Actual

El roadmap fue actualizado el 2026-06-06 como roadmap maestro basado en una
lectura del proyecto. El modulo ya esta mas alla de un MVP basico: tiene
arquitectura modular, persistencia, UI MVC, API versionada, wizard, evidencias,
facts, intake externo, PDF, feed de integracion y AI stub.

La prioridad recomendada quedo definida como P0: endurecimiento producto
standalone para piloto industrial.

## Decisiones

- El roadmap debe conservar vision de plataforma, pero priorizar primero el
  producto standalone.
- Las fases se expresan como P0/P1/P2/P3/P4 con criterios de salida.
- El siguiente corte recomendado es seguridad, tenant real, tests, auditoria y
  smoke confiable antes de invertir fuerte en UI premium.
- La UI cockpit queda como P1, despues del endurecimiento base.

## Cambios Realizados

- Reescrito `docs/ROADMAP.md` como roadmap maestro.
- Agregada lectura ejecutiva.
- Agregado estado comprobado por frente: arquitectura, producto, wizard,
  evidencias, facts, intake, APIs, IA, UI, QA, seguridad.
- Agregadas prioridades P0 a P4.
- Agregados criterios de salida por prioridad.
- Agregados riesgos principales.
- Agregado proximo corte recomendado.

## Pendientes

- Convertir P0 en spec ejecutable.
- Crear bitacoras iniciales de `BACKEND`, `DB`, `QA` y `DEVOPS`.
- Cuando se implemente P0, actualizar `STATUS_AND_NEXT_STEPS.md` y
  `VALIDATION_LOG.md`.

## Riesgos

- El roadmap puede crecer demasiado si se usa como backlog detallado. Debe
  seguir siendo direccion y criterios de salida.
- Los detalles de implementacion deben pasar a specs, no vivir todos en el
  roadmap.

## Validaciones

- Revision manual de documentos principales.
- Revision de estructura `src`, `tests` y `docs`.
- Revision de controllers API/MVC, entidades, servicios, contratos y tests
  livianos.
- No se ejecuto build porque el cambio fue documental.

## Ultimo Cierre

- Fecha: 2026-06-06.
- Resumen: roadmap maestro actualizado a partir del estado real del proyecto.
- Commit sugerido: `docs(roadmap): align roadmap with project state`.
