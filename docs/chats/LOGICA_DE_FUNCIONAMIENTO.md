# Chat Logica de Funcionamiento

## Alcance

Mantener la memoria funcional pura del modulo Ishikawa RCA: que hace, como lo
hace, que reglas lo gobiernan, que falta y que debe actualizarse cuando el
producto avance.

Este chat no se enfoca en codigo fuente ni implementacion tecnica fina, salvo
cuando sea necesario leerla para reconstruir o validar comportamiento funcional.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/LOGICA_DE_FUNCIONAMIENTO.md`
- Documentos especificos del dominio afectado:
  - `docs/API_CONTRACTS.md` si cambia API, contratos, snapshots o eventos.
  - `docs/AI_INTEGRATION.md` si cambia IA.
  - `docs/EXTERNAL_CLAIM_INTAKE.md` si cambia intake externo.
  - `docs/UI_CONTENT_BENCHMARK.md` si cambia experiencia UI.
  - `docs/VALIDATION_LOG.md` si hubo validaciones.

## Regla de Actualizacion Continua

Cada vez que se avance en una funcionalidad, este frente debe actualizar:

- `docs/LOGICA_DE_FUNCIONAMIENTO.md` si cambia la logica funcional.
- Esta bitacora si se toma una decision, se detecta un faltante o cambia el
  estado del frente.
- `docs/VALIDATION_LOG.md` si se ejecutan validaciones.
- `docs/ROADMAP.md` o `docs/STATUS_AND_NEXT_STEPS.md` si cambia el estado
  general o prioridades.

## Estado Actual

- Documento funcional central creado en `docs/LOGICA_DE_FUNCIONAMIENTO.md`.
- La logica actual del modulo ya esta resumida por dominios: RCA, canvas,
  acciones, evidencias, facts, wizard, cierre, 8D, intake externo, integracion,
  IA, UI, seguridad y QA.
- El documento diferencia capacidades implementadas de pendientes.

## Decisiones

- El nombre del frente queda normalizado como `LOGICA_DE_FUNCIONAMIENTO`.
- La documentacion funcional viva queda en `docs/LOGICA_DE_FUNCIONAMIENTO.md`.
- Cada avance futuro debe actualizar este documento si altera reglas, flujos,
  validaciones, estados, contratos o pendientes funcionales.

## Cambios Realizados

- Creado `docs/LOGICA_DE_FUNCIONAMIENTO.md`.
- Creada esta bitacora tematica.

## Pendientes

- Vincular este frente desde otros documentos si se decide crear un indice
  global de documentacion.
- Mantener sincronizada la logica funcional con cambios de backend, UI, API,
  DB, QA, seguridad, IA e integracion.

## Riesgos

- Si se cambia codigo o contratos sin actualizar este documento, la memoria
  funcional queda desfasada.
- Si se documentan reglas no implementadas como si existieran, se pierde
  confiabilidad. Todo supuesto debe quedar marcado como pendiente o decision.

## Validaciones

- Validacion documental/estatica: lectura de contexto obligatorio y documentos
  base del repo.
- No se ejecuto build ni tests porque el cambio fue solo documental.

## Ultimo Cierre

- Fecha: 2026-06-06
- Resumen: Se creo la documentacion viva de logica de funcionamiento y su
  bitacora tematica.
- Commit: pendiente.
