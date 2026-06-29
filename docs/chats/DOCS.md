# Chat Documentacion

## Alcance

Mantener la documentacion operativa del proyecto: contexto maestro, roadmap,
estado, reglas de chats Codex, bitacoras tematicas, specs, ADRs y registros de
validacion documental.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/chats/DOCS.md`

Si la documentacion toca UI, backend, base de datos, QA o DevOps, leer tambien
la bitacora tematica correspondiente.

## Estado Actual

El proyecto ya tenia documentacion de contexto, roadmap, contratos,
integracion, limites del modulo, operaciones locales, benchmark UI y log de
validaciones. Faltaba una regla explicita para conectar chats separados usando
Markdown como memoria compartida.

## Decisiones

- La conexion entre chats se hara mediante `docs/chats/<CHAT>.md`.
- `AGENTS.md` contiene las reglas obligatorias de inicio y cierre.
- `docs/CODEX_CHAT_OPERATING_MODEL.md` contiene el modelo operativo completo.
- Cada chat tematico debe actualizar su bitacora antes de cerrar.

## Cambios Realizados

- Creado `AGENTS.md`.
- Creado `docs/CODEX_CHAT_OPERATING_MODEL.md`.
- Creado `docs/chats/_TEMPLATE.md`.
- Creado `docs/chats/UI.md`.
- Creado `docs/chats/DOCS.md`.
- Actualizado `docs/ROADMAP.md` como roadmap maestro basado en el estado real
  del proyecto.
- Creado `docs/chats/ROADMAP.md`.
- Creado `docs/CODEX_COORDINATOR_START.md` como documento de arranque del chat
  coordinador.
- Creado `docs/CODEX_TOPIC_CHAT_START.md` como documento reusable de arranque
  para chats tematicos por foco.
- Creado `docs/backend.md` como memoria viva del backend.
- Creado `docs/chats/BACKEND.md` como bitacora tematica Backend.
- Actualizado `docs/CODEX_CHAT_OPERATING_MODEL.md` para exigir lectura y
  actualizacion de `docs/backend.md` cuando cambie backend.

## Pendientes

- Crear bitacoras iniciales para `DB`, `QA`, `DEVOPS`, `AI` y `SEGURIDAD`
  cuando se abra cada chat o cuando se quiera dejar todo el sistema prearmado.
- Evaluar si conviene crear `docs/SPECS/` y `docs/ADR/` con plantillas reales.
- Definir si el chat central debe actualizar siempre `STATUS_AND_NEXT_STEPS.md`
  luego de cada cierre tematico.

## Riesgos

- Si los chats no actualizan su bitacora, el sistema vuelve a depender del
  historial conversacional.
- Si se actualizan demasiados documentos por cada cambio pequeno, el flujo se
  vuelve pesado. La regla debe aplicarse con criterio: bitacora siempre,
  roadmap/status/validation solo cuando corresponda.

## Validaciones

- Revision manual de archivos creados.
- `git status --short` usado para confirmar archivos nuevos y cambios previos
  no relacionados.

## Ultimo Cierre

- Fecha: 2026-06-06.
- Resumen: agregado modelo operativo para chats Codex, actualizado roadmap
  maestro y creados documentos de arranque para coordinador y chats tematicos.
- Commit sugerido: `docs(codex): add chat start guides`.

## Cierre 2026-06-08

- Resumen: creado documento vivo backend, bitacora Backend y regla de
  actualizacion continua para cambios backend.
- Validacion: revision documental manual; no se ejecuto build ni tests por ser
  cambio solo de documentacion.
- Commit sugerido: `docs(backend): add backend operating notes`.

## Cierre 2026-06-18

- Resumen: creado `Faltantes.md` en la raiz del repo para consolidar pendientes
  accionables locales, pendientes bloqueados por plataforma externa,
  validaciones operativas y limpieza de workspace.
- Validacion: revision documental manual; no se ejecuto build ni tests por ser
  cambio solo de documentacion.
- Commit sugerido: `docs(roadmap): add remaining work summary`.
