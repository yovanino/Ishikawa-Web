# Arranque de Chat Tematico

Usar este documento para abrir un chat por tema. El foco se puede cambiar cada
vez: UI, Backend, DB, QA, DevOps, Roadmap, Docs, AI, API, Seguridad u otro
frente especifico.

La idea es que cada chat tematico trabaje profundo en un area, pero se conecte
con el proyecto mediante su bitacora en `docs/chats/<FOCO>.md`.

## Variables

Completar al iniciar:

```text
FOCO = <UI | BACKEND | DB | QA | DEVOPS | ROADMAP | DOCS | AI | API | SEGURIDAD | OTRO>
OBJETIVO = <que se quiere lograr>
ALCANCE = <que incluye>
FUERA_DE_ALCANCE = <que no debe tocar>
VALIDACION_ESPERADA = <build, tests, smoke, revision documental, browser QA, etc>
```

## Mensaje Inicial Sugerido

```text
Actua como Chat Tematico con foco en <FOCO> para el modulo Ishikawa RCA.

Objetivo:
<OBJETIVO>

Alcance:
<ALCANCE>

Fuera de alcance:
<FUERA_DE_ALCANCE>

Antes de modificar archivos, lee:

- AGENTS.md
- docs/MASTER_CONTEXT.md
- docs/ROADMAP.md
- docs/STATUS_AND_NEXT_STEPS.md
- docs/MODULE_BOUNDARIES.md
- docs/CODEX_CHAT_OPERATING_MODEL.md
- docs/chats/<FOCO>.md si existe

Tambien lee los documentos especificos del foco.

Despues de leer, responde con:

1. Contexto relevante.
2. Archivos o areas probablemente afectadas.
3. Riesgos.
4. Plan corto.
5. Validaciones previstas.

Luego ejecuta la tarea. Al cerrar, actualiza docs/chats/<FOCO>.md y los
documentos globales que correspondan.
```

## Documentos Especificos por Foco

### UI

Leer:

- `docs/UI_CONTENT_BENCHMARK.md`
- `docs/chats/UI.md`
- Views, view models, CSS y JS afectados.

Actualizar:

- `docs/chats/UI.md`
- `docs/VALIDATION_LOG.md` si hubo validacion visual/funcional.
- `docs/ROADMAP.md` si cambia el estado de una capacidad UI.

### Backend

Leer:

- `docs/API_CONTRACTS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/chats/BACKEND.md` si existe.
- Servicios de `Application` e `Infrastructure` afectados.

Actualizar:

- `docs/chats/BACKEND.md`
- `docs/API_CONTRACTS.md` si cambia un contrato.
- `docs/VALIDATION_LOG.md` si se ejecutan tests/build/API smoke.

### DB

Leer:

- `docs/API_CONTRACTS.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/chats/DB.md` si existe.
- Entidades, `RcaDbContext` y migraciones.

Actualizar:

- `docs/chats/DB.md`
- `docs/VALIDATION_LOG.md` si se crean/aplican migraciones.
- `docs/STATUS_AND_NEXT_STEPS.md` si quedan pendientes de DB.

### QA

Leer:

- `docs/VALIDATION_LOG.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/chats/QA.md` si existe.
- Proyecto `tests`.

Actualizar:

- `docs/chats/QA.md`
- `docs/VALIDATION_LOG.md`
- Specs/checklists si se detectan brechas.

### DevOps

Leer:

- `docs/LOCAL_OPERATIONS.md`
- scripts en `scripts/`
- `.github/` si aplica.
- `docs/chats/DEVOPS.md` si existe.

Actualizar:

- `docs/chats/DEVOPS.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/VALIDATION_LOG.md` si cambia build, CI/CD o entorno.

### Roadmap

Leer:

- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/ROADMAP.md`
- estructura real de `src`, `tests` y `docs`.

Actualizar:

- `docs/chats/ROADMAP.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md` si cambia el proximo corte.

### Docs

Leer:

- `docs/chats/DOCS.md`
- documentos afectados.

Actualizar:

- `docs/chats/DOCS.md`
- documentos creados/modificados.
- indice o README si se agregan documentos importantes.

### AI

Leer:

- `docs/AI_INTEGRATION.md`
- `docs/API_CONTRACTS.md`
- contratos y servicios en `Application/Ai`.

Actualizar:

- `docs/chats/AI.md` si existe o crearlo desde plantilla.
- `docs/AI_INTEGRATION.md` si cambia arquitectura/contrato.
- `docs/API_CONTRACTS.md` si cambia endpoint o payload.

### Seguridad

Leer:

- `docs/MODULE_BOUNDARIES.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/EXTERNAL_CLAIM_INTAKE.md`
- controllers, formularios y servicios afectados.

Actualizar:

- `docs/chats/SEGURIDAD.md` si existe o crearlo desde plantilla.
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/VALIDATION_LOG.md` si se validan permisos/auth.

## Plantilla de Bitacora del Chat

Si `docs/chats/<FOCO>.md` no existe, crearla usando esta estructura:

```markdown
# Chat <FOCO>

## Alcance

<Que cubre este chat.>

## Regla de Inicio

Leer antes de trabajar:

- AGENTS.md
- docs/MASTER_CONTEXT.md
- docs/ROADMAP.md
- docs/STATUS_AND_NEXT_STEPS.md
- docs/MODULE_BOUNDARIES.md
- docs/CODEX_CHAT_OPERATING_MODEL.md
- docs/chats/<FOCO>.md
- <docs especificos>

## Estado Actual

- <estado del frente>

## Decisiones

- <decisiones tomadas>

## Cambios Realizados

- <cambios hechos>

## Pendientes

- <pendientes>

## Riesgos

- <riesgos>

## Validaciones

- <validaciones ejecutadas o motivo si no se ejecutaron>

## Ultimo Cierre

- Fecha:
- Resumen:
- Commit:
```

## Regla de Cierre

Antes de cerrar el chat tematico:

1. Actualizar `docs/chats/<FOCO>.md`.
2. Registrar decisiones y cambios.
3. Registrar validaciones ejecutadas.
4. Actualizar `docs/VALIDATION_LOG.md` si hubo build, tests, smoke, DB,
   browser QA o validacion relevante.
5. Actualizar `docs/ROADMAP.md` o `docs/STATUS_AND_NEXT_STEPS.md` si cambia el
   estado del proyecto.
6. Sugerir commit convencional.

## Formato de Cierre Sugerido

```text
Resumen:
- <que se hizo>

Archivos modificados:
- <archivo 1>
- <archivo 2>

Validacion:
- <comando o revision>
- <resultado>

Documentacion actualizada:
- docs/chats/<FOCO>.md
- <otros docs>

Riesgos / pendientes:
- <riesgo o pendiente>

Commit sugerido:
- <tipo>(<foco>): <mensaje>
```
