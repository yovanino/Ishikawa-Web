# Chat Coordinacion

## Alcance

Coordinar agentes y chats tematicos del modulo Ishikawa RCA, actuando como
chat central / PM arquitecto segun `docs/CODEX_CHAT_OPERATING_MODEL.md`.

Este chat debe:

- Mantener vision global, prioridades y cortes de trabajo.
- Derivar tareas hacia chats tematicos cuando convenga.
- Consolidar decisiones, riesgos y entregables.
- Verificar que los chats tematicos actualicen sus bitacoras en `docs/chats`.
- Mantener alineados `ROADMAP.md`, `STATUS_AND_NEXT_STEPS.md` y
  `VALIDATION_LOG.md` cuando corresponda.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/chats/COORDINACION.md`
- Bitacoras tematicas afectadas por la tarea.
- Documentos especificos del dominio cuando la coordinacion toque API, datos,
  UI, validaciones, operaciones o roadmap.

## Estado Actual

- El modulo Ishikawa RCA esta en estado standalone operativo, con persistencia,
  wizard, evidencias, PDF, integraciones REST/versionadas, timeline y contratos
  de asistencia IA stub.
- El roadmap maestro vigente define como proximo corte recomendado P0:
  endurecimiento producto standalone para piloto industrial.
- Existen bitacoras tematicas iniciales para ROADMAP, UI y documentacion.
- Falta crear bitacoras tematicas cuando se abran frentes Backend, DB, QA o
  DevOps.

## Decisiones

- 2026-06-06: Este chat queda designado como chat central de coordinacion de
  agentes.
- 2026-06-06: La memoria compartida del chat central queda en
  `docs/chats/COORDINACION.md`.
- 2026-06-06: Para el siguiente corte, la coordinacion prioriza P0
  (seguridad/tenant, tests, auditoria, smoke y hardening) antes de nuevas
  mejoras visuales P1 o integraciones P2.
- 2026-06-11: Para debug manual, inspeccion visual y futuras pruebas UI
  locales, usar Firefox Developer Edition por defecto cuando se necesite un
  navegador externo.
- 2026-06-18: Codex ejecuta build, tests, smokes y servidores locales cuando
  correspondan para validar cambios tecnicos.
- 2026-06-18: La validacion en Browser, navegadores externos o browser QA queda
  a cargo del usuario desde Visual Studio 2026, salvo pedido explicito para que
  Codex lo haga como excepcion.

## Cambios Realizados

- 2026-06-06: Se creo la bitacora de coordinacion central.
- 2026-06-06: Se registro el arranque formal del chat coordinador y la
  prioridad P0 como frente recomendado.
- 2026-06-11: Se derivo la preferencia de navegador a frente DevOps/QA liviano
  y se registro en `docs/LOCAL_OPERATIONS.md` y `docs/chats/DEVOPS.md`.
- 2026-06-18: Se corrigio la regla de validacion: Codex mantiene validacion
  tecnica por build/tests/smokes/servidores locales, y el usuario mantiene la
  validacion Browser desde VS 2026.

## Pendientes

- Convertir P0 en specs ejecutables por frente tematico.
- Crear bitacoras tematicas adicionales cuando se inicien frentes Backend, DB,
  QA o DevOps.
- Mantener trazabilidad cruzada entre decisiones de coordinacion y bitacoras
  tematicas.
- Definir si Seguridad queda como chat propio o como subfrente Backend/DB para
  el primer corte P0.
- Si se implementan pruebas MVC/UI automatizadas, asegurar que la configuracion
  use Firefox Developer Edition como browser de debug por defecto.
- En cierres futuros, distinguir validaciones tecnicas ejecutadas por Codex de
  validaciones Browser pendientes de ejecucion por el usuario en VS 2026.

## Riesgos

- Perder decisiones si los chats tematicos no cierran con actualizacion de
  bitacora.
- Mezclar implementaciones profundas en este chat central sin derivarlas a un
  frente tematico cuando el alcance crezca.
- Desalinear roadmap/estado si se completan cambios funcionales sin actualizar
  `docs`.

## Validaciones

- 2026-06-06: Lectura de contexto obligatorio completada.
- 2026-06-06: No se ejecuto build/test porque el cambio es documental.
- 2026-06-06: Revisadas bitacoras ROADMAP, UI y DOCS; confirmada ausencia de
  BACKEND, DB, QA y DEVOPS.
- 2026-06-11: Confirmado path local de Firefox Developer Edition en
  `C:\Program Files\Firefox Developer Edition\firefox.exe`.
- 2026-06-18: No se ejecuto build/test/smoke porque el cambio es documental.
- 2026-06-18: No se ejecuto Browser ni navegador externo; la regla indica que
  esas validaciones las ejecuta el usuario desde VS 2026 salvo pedido explicito.

## Ultimo Cierre

- Fecha: 2026-06-18
- Resumen: Corregida regla: Codex valida tecnico; usuario valida Browser desde
  Visual Studio 2026.
- Commit: Pendiente.
