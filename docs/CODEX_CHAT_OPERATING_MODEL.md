# Modelo Operativo de Chats Codex

Fecha: 2026-06-06.

## Objetivo

Definir como se coordinan el chat central y los chats tematicos de Codex usando
documentos Markdown como punto de conexion persistente.

El principio base es simple: cada chat puede trabajar de forma separada, pero
debe entrar y salir por `docs`. La documentacion es la memoria compartida del
proyecto.

## Arquitectura de Trabajo

```text
Chat Central / PM Arquitecto
  |
  +-- docs/MASTER_CONTEXT.md
  +-- docs/ROADMAP.md
  +-- docs/STATUS_AND_NEXT_STEPS.md
  +-- docs/CODEX_CHAT_OPERATING_MODEL.md
  |
  +-- Chat UI          -> docs/chats/UI.md
  +-- Chat Backend     -> docs/chats/BACKEND.md
  +-- Chat DB          -> docs/chats/DB.md
  +-- Chat QA          -> docs/chats/QA.md
  +-- Chat DevOps      -> docs/chats/DEVOPS.md
  +-- Chat Docs        -> docs/chats/DOCS.md
```

## Responsabilidad del Chat Central

El chat central actua como PM, arquitecto y memoria de gobierno.

Debe:

- Mantener la vision global del modulo.
- Definir prioridades y cortes de trabajo.
- Crear o aprobar specs.
- Coordinar chats tematicos.
- Resolver decisiones arquitectonicas.
- Consolidar entregables.
- Mantener roadmap, estado y pendientes.
- Revisar que cada chat tematico deje trazabilidad en `docs`.

No debe:

- Mezclar muchas tareas tecnicas profundas en un mismo hilo si conviene abrir
  un chat tematico.
- Perder decisiones importantes en conversacion sin pasarlas a Markdown.
- Aceptar cambios grandes sin spec, criterio de aceptacion o validacion.

## Responsabilidad de Cada Chat Tematico

Cada chat tematico tiene una bitacora propia en `docs/chats/<CHAT>.md`.

Esa bitacora debe contener:

- Alcance del chat.
- Documentos que debe leer al inicio.
- Estado actual del tema.
- Decisiones tomadas.
- Cambios realizados.
- Pendientes.
- Riesgos.
- Validaciones ejecutadas.
- Ultimo commit relacionado, si existe.

El archivo del chat no reemplaza `ROADMAP.md` ni `VALIDATION_LOG.md`; los
complementa. La bitacora tematica es la memoria local del frente de trabajo.

## Regla de Inicio por Chat

Al iniciar un chat tematico, Codex debe ejecutar esta regla:

```text
1. Leer AGENTS.md.
2. Leer docs/MASTER_CONTEXT.md.
3. Leer docs/ROADMAP.md.
4. Leer docs/STATUS_AND_NEXT_STEPS.md.
5. Leer docs/MODULE_BOUNDARIES.md.
6. Leer docs/CODEX_CHAT_OPERATING_MODEL.md.
7. Leer docs/chats/<CHAT>.md si existe.
8. Leer documentos especificos del dominio.
9. Resumir contexto, riesgos y proximo paso antes de modificar codigo.
```

## Documentos por Tipo de Chat

### Chat UI

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/UI_CONTENT_BENCHMARK.md`
- `docs/chats/UI.md`

Debe actualizar:

- `docs/chats/UI.md`
- `docs/ROADMAP.md` si cambia el estado de funcionalidades UI.
- `docs/VALIDATION_LOG.md` si ejecuta validaciones relevantes.

### Chat Backend

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/backend.md`
- `docs/API_CONTRACTS.md`
- `docs/AI_INTEGRATION.md` si toca asistencia IA.
- `docs/chats/BACKEND.md`

Debe actualizar:

- `docs/backend.md` si cambia codigo, contratos, validaciones, seguridad,
  persistencia, auditoria, integraciones o reglas tecnicas del backend.
- `docs/chats/BACKEND.md`
- `docs/API_CONTRACTS.md` si cambia un contrato.
- `docs/VALIDATION_LOG.md` si valida build, tests o endpoints.

### Chat Base de Datos

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/API_CONTRACTS.md`
- `docs/chats/DB.md`

Debe actualizar:

- `docs/chats/DB.md`
- `docs/VALIDATION_LOG.md` si crea/aplica migraciones.
- `docs/STATUS_AND_NEXT_STEPS.md` si quedan migraciones o credenciales
  pendientes.

### Chat QA

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/QA.md`

Debe actualizar:

- `docs/chats/QA.md`
- `docs/VALIDATION_LOG.md`
- Specs o checklist de aceptacion cuando encuentre brechas.

### Chat DevOps

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/chats/DEVOPS.md`

Debe actualizar:

- `docs/chats/DEVOPS.md`
- `docs/LOCAL_OPERATIONS.md`
- `docs/VALIDATION_LOG.md` si cambia build, deploy, CI/CD o entorno.

### Chat Documentacion

Debe leer:

- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/chats/DOCS.md`

Debe actualizar:

- `docs/chats/DOCS.md`
- Documentos afectados en `docs`.
- Indice o README si se agregan documentos importantes.

## Regla de Cierre por Chat

Antes de cerrar una tarea, el chat tematico debe:

1. Actualizar su bitacora `docs/chats/<CHAT>.md`.
2. Registrar decisiones nuevas.
3. Registrar archivos modificados.
4. Registrar validaciones ejecutadas o explicar por que no se ejecutaron.
5. Actualizar roadmap, estado o validation log si corresponde.
6. Proponer o crear commit con mensaje convencional.

## Regla de Validacion Tecnica y Browser

Codex debe ejecutar build, tests, smokes y servidores locales cuando
correspondan para validar cambios tecnicos, respetando timeouts explicitos,
plan de apagado y cierre limpio de procesos.

La validacion en Browser, navegadores externos o browser QA queda a cargo del
usuario desde Visual Studio 2026. Codex no debe usar Browser ni abrir
navegadores para validar UI por iniciativa propia, salvo pedido explicito del
usuario para esa tarea.

Codex debe:

- Registrar builds/tests/smokes ejecutados por Codex cuando correspondan.
- Indicar claramente si la validacion Browser queda pendiente del usuario.
- Entregar checklist o pasos esperados para VS 2026 cuando ayuden al cierre.

## Regla de Timeouts y Procesos Locales

Todo comando largo debe ejecutarse con timeout explicito. Antes de iniciar un
servidor local, watcher, smoke test o proceso en background, Codex debe definir
como lo va a detener y verificar al cierre que no quede corriendo. Si una
validacion queda lenta o colgada, debe cortar el proceso iniciado, documentar la
limitacion y reemplazarla por una validacion mas acotada cuando sea suficiente.

No se deben dejar procesos `dotnet run`, servidores web, watchers ni sesiones
de browser automatizado activos al terminar una tarea.

Formato recomendado de cierre:

```text
Resumen:
- Que cambio.
- Por que cambio.
- Archivos modificados.

Validacion:
- Comando ejecutado.
- Resultado.

Documentacion actualizada:
- docs/chats/<CHAT>.md
- Otros docs afectados.

Commit sugerido:
- feat(ui): agregar panel de validacion RCA
```

## Politica de Commits

Usar Conventional Commits:

- `feat(<area>): ...`
- `fix(<area>): ...`
- `refactor(<area>): ...`
- `test(<area>): ...`
- `docs(<area>): ...`
- `chore(<area>): ...`
- `perf(<area>): ...`

El area debe coincidir con el frente de trabajo cuando sea posible:

- `ui`
- `backend`
- `db`
- `qa`
- `devops`
- `docs`
- `roadmap`
- `api`
- `ai`

Ejemplos:

```text
docs(codex): define chat operating model
feat(ui): add RCA validation panel
fix(api): preserve external fact idempotency
test(qa): add smoke coverage for RCA facts
```

## Criterio de Calidad

Un chat esta correctamente cerrado si otra conversacion puede continuar el
trabajo leyendo solamente:

1. `AGENTS.md`
2. `docs/MASTER_CONTEXT.md`
3. `docs/ROADMAP.md`
4. `docs/STATUS_AND_NEXT_STEPS.md`
5. `docs/CODEX_CHAT_OPERATING_MODEL.md`
6. `docs/chats/<CHAT>.md`

Si para entender el estado real hace falta buscar en el historial del chat,
entonces la documentacion de cierre fue insuficiente.
