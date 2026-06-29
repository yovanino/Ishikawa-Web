# Reglas de Trabajo con Codex

Este repositorio usa un modelo de trabajo coordinado por documentacion. Cada
chat tematico debe conectarse con el resto del proyecto mediante archivos `.md`
en `docs`.

## Regla Obligatoria de Inicio

Al comenzar cualquier chat o tarea, Codex debe:

1. Leer `docs/MASTER_CONTEXT.md`.
2. Leer `docs/ROADMAP.md`.
3. Leer `docs/STATUS_AND_NEXT_STEPS.md`.
4. Leer `docs/MODULE_BOUNDARIES.md`.
5. Leer `docs/CODEX_CHAT_OPERATING_MODEL.md`.
6. Leer la bitacora del chat tematico si existe, por ejemplo
   `docs/chats/UI.md`, `docs/chats/BACKEND.md`, `docs/chats/QA.md` o
   `docs/chats/DB.md`.

Si la tarea afecta contratos API, datos, UI, validaciones o roadmap, tambien
debe leer el documento especifico correspondiente dentro de `docs`.

## Regla Obligatoria de Cierre

Antes de cerrar una tarea, Codex debe:

1. Actualizar la bitacora del chat tematico en `docs/chats/<CHAT>.md`.
2. Actualizar `docs/ROADMAP.md`, `docs/STATUS_AND_NEXT_STEPS.md` o
   `docs/VALIDATION_LOG.md` cuando corresponda.
3. Explicar archivos modificados, riesgos y validaciones ejecutadas.
4. Preparar un commit con mensaje convencional cuando el usuario lo pida o el
   flujo de trabajo lo requiera.

## Restricciones

- No inventar APIs, modelos, tablas ni reglas de negocio.
- Mantener compatibilidad con el alcance standalone de Ishikawa RCA.
- Documentar supuestos cuando falte informacion.
- No acoplar este modulo directamente a otros modulos futuros; usar APIs,
  snapshots, eventos o contratos versionados.

## Regla de Validacion Tecnica y Browser

- Codex debe ejecutar build, tests, smokes y servidores locales cuando
  correspondan para validar cambios tecnicos, respetando timeouts y apagado de
  procesos.
- La validacion en Browser, navegadores externos o browser QA la ejecuta el
  usuario desde Visual Studio 2026.
- Codex no debe usar Browser ni abrir navegadores para validar UI por
  iniciativa propia, salvo pedido explicito del usuario para esa tarea.
- Codex debe registrar en el cierre que la validacion Browser queda pendiente
  del usuario cuando aplique.

## Regla de Timeouts y Procesos Locales

- Todo comando largo debe ejecutarse con timeout explicito.
- Todo servidor, watcher, smoke o proceso en background debe tener plan de
  apagado antes de iniciarse.
- Si una validacion local se cuelga, Codex debe detener el proceso iniciado,
  registrar la limitacion y usar una validacion mas acotada.
- No dejar procesos `dotnet run`, servidores web, watchers ni navegadores
  automatizados corriendo al cerrar una tarea.
