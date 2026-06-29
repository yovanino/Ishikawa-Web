# Chat DevOps

## Alcance

Coordinar entorno local, scripts de ejecucion, validaciones operativas,
herramientas de debug/test, CI/CD futuro y documentacion de operacion del
modulo Ishikawa RCA.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/DEVOPS.md`

Si el cambio afecta APIs, base de datos, UI o QA, leer tambien la bitacora
tematica correspondiente.

## Estado Actual

- El modulo cuenta con scripts locales para preflight de SDK, arranque web,
  smokes API, validacion de autorizacion, validacion de modelos, adjuntos,
  auditoria, facts externos y validacion local completa.
- La validacion local recomendada sigue siendo `scripts/run-local-validation.ps1`
  con `-Build` cuando haya cambios backend significativos.
- Para debug manual y UI QA local, Firefox Developer Edition queda como
  navegador externo por defecto.

## Decisiones

- 2026-06-11: Usar Firefox Developer Edition como navegador por defecto para
  debug manual, inspeccion visual y futuras pruebas UI locales.
- 2026-06-18: Codex ejecuta build, tests, smokes y servidores locales cuando
  correspondan para validar cambios tecnicos.
- 2026-06-18: La validacion en Browser, navegadores externos o browser QA queda
  a cargo del usuario desde Visual Studio 2026, salvo pedido explicito para que
  Codex lo haga como excepcion.

## Cambios Realizados

- 2026-06-11: Creada bitacora DevOps.
- 2026-06-11: Documentada preferencia de Firefox Developer Edition en
  `docs/LOCAL_OPERATIONS.md`.
- 2026-06-18: Documentada regla operativa corregida en
  `docs/LOCAL_OPERATIONS.md`: Codex valida tecnico y el usuario valida Browser.

## Pendientes

- Si se agregan pruebas MVC/UI automatizadas, configurar la herramienta elegida
  para usar Firefox Developer Edition como browser de debug por defecto.
- Definir CI/CD cuando el proyecto necesite validacion remota.
- Mantener comandos y checklists tecnicos listos para ejecucion por Codex y
  pasos Browser claros para ejecucion del usuario desde Visual Studio 2026.

## Riesgos

- Las pruebas API actuales no verifican rendering real en navegador; Firefox
  Developer Edition aplica a debug/UI QA, no reemplaza smokes API.
- El path local puede variar en otra maquina; documentar override si aparece
  un segundo entorno de desarrollo.
- Puede quedar menor evidencia visual automatica en cierres de Codex; mitigar
  registrando claramente "validacion Browser pendiente por usuario" cuando
  aplique.

## Validaciones

- 2026-06-11: Confirmado path local:
  `C:\Program Files\Firefox Developer Edition\firefox.exe`.
- 2026-06-11: No se ejecuto build/test porque el cambio es documental y de
  convencion operativa.
- 2026-06-18: No se ejecuto build/test/smoke porque el cambio es documental.
- 2026-06-18: No se ejecuto Browser ni navegador externo porque la validacion
  Browser queda a cargo del usuario desde VS 2026 salvo pedido explicito.

## Ultimo Cierre

- Fecha: 2026-06-18.
- Resumen: Corregida regla operativa de validacion tecnica y Browser.
- Commit: Pendiente.
