# Chat QA

## Alcance

Coordinar validaciones, criterios de aceptacion, pruebas funcionales,
regresiones, smokes, evidencias de prueba y brechas de calidad del modulo
Ishikawa RCA.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/QA.md`

Si la validacion afecta UI, backend, base de datos, DevOps o contratos API,
leer tambien la bitacora tematica correspondiente.

## Estado Actual

- El proyecto tiene smokes API y validaciones locales documentadas.
- El roadmap marca tests MVC/UI como pendiente post-P0.
- Codex ejecuta build, tests, smokes y servidores locales cuando correspondan.
- La validacion en Browser, navegadores externos o browser QA queda a cargo del
  usuario desde Visual Studio 2026 por regla operativa vigente.

## Decisiones

- 2026-06-18: Codex ejecuta build, tests, smokes y servidores locales cuando
  correspondan para validar cambios tecnicos.
- 2026-06-18: La validacion en Browser, navegadores externos o browser QA queda
  a cargo del usuario desde Visual Studio 2026, salvo pedido explicito para que
  Codex lo haga como excepcion.

## Cambios Realizados

- 2026-06-18: Creada bitacora QA.
- 2026-06-18: Corregida regla de validacion: Codex valida tecnico; usuario
  valida Browser desde VS 2026.

## Pendientes

- Definir checklist QA para los proximos cortes funcionales.
- Mantener `docs/VALIDATION_LOG.md` actualizado con validaciones tecnicas de
  Codex y resultados Browser reportados por el usuario desde VS 2026.
- Crear cobertura MVC/UI automatizada cuando se decida el enfoque.

## Riesgos

- Si Codex no ejecuta validaciones tecnicas necesarias, pueden quedar
  regresiones no detectadas.
- Si el resultado de una validacion Browser de usuario no se documenta, se
  pierde trazabilidad visual del cierre.

## Validaciones

- 2026-06-18: No se ejecuto build/test/smoke porque el cambio es documental.
- 2026-06-18: No se ejecuto Browser ni browser QA porque queda a cargo del
  usuario desde VS 2026 salvo pedido explicito.

## Ultimo Cierre

- Fecha: 2026-06-18.
- Resumen: Corregida regla QA: Codex valida tecnico; usuario valida Browser.
- Commit: Pendiente.
