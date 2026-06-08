# Backend

Fecha de corte: 2026-06-08.

Este documento es la memoria operativa viva del backend del modulo Ishikawa
RCA. Debe actualizarse cada vez que cambie codigo, contratos, validaciones,
seguridad, persistencia, auditoria, integraciones o reglas tecnicas del
backend.

## Regla de Actualizacion Continua

Actualizar este archivo cuando una tarea modifique:

- Controllers MVC/API, endpoints `/api/v1` o formato de respuesta.
- Servicios de aplicacion, politicas de dominio o validaciones backend.
- Entidades, DbContext, migraciones, repositorios o persistencia.
- Seguridad, autenticacion, autorizacion, tenant, roles o permisos.
- Evidencias, adjuntos, storage, hash, descarga o validacion documental.
- Facts operacionales, idempotencia, snapshots o feed de integracion.
- Intake externo cliente/proveedor.
- IA asistida, cliente AI Gateway o fallback stub.
- Auditoria, manejo de errores, logging o trazabilidad.
- Tests backend, smoke API/DB, scripts o configuracion operativa.

La bitacora del frente backend es `docs/chats/BACKEND.md` y tambien debe
actualizarse al cerrar cada tarea backend. Si el cambio altera contratos
publicos, actualizar `docs/API_CONTRACTS.md`. Si cambia logica funcional,
actualizar `docs/LOGICA_DE_FUNCIONAMIENTO.md`. Si se ejecutan validaciones,
registrarlas en `docs/VALIDATION_LOG.md`.

## Lectura Obligatoria para Trabajar Backend

Antes de modificar backend, leer:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/backend.md`
- `docs/API_CONTRACTS.md`
- `docs/LOGICA_DE_FUNCIONAMIENTO.md`
- `docs/AI_INTEGRATION.md` si se toca IA.
- `docs/EXTERNAL_CLAIM_INTAKE.md` si se toca intake externo.
- `docs/chats/BACKEND.md`

## Estado Actual

El backend ya cuenta con:

- Solucion ASP.NET Core MVC/API por capas.
- Proyectos separados `Domain`, `Application`, `Contracts`,
  `Infrastructure` y `Web`.
- Persistencia EF Core + MySQL.
- Implementacion in-memory para pruebas/logica aislada.
- Endpoints versionados `/api/v1`.
- Incidentes RCA, ramas, causas, subcausas, acciones, evidencias, facts,
  wizard, cierre formal y escalamiento 8D.
- Adjuntos reales de evidencia con storage local configurable, descarga
  controlada y SHA-256.
- Intake externo cliente/proveedor con token hasheado, expiracion, revocacion,
  revision interna, rechazo e importacion opcional.
- Snapshots y feed derivado de eventos para integracion con otros modulos.
- Contratos de IA y cliente stub local.
- Tests livianos para politica de resolucion y facts externos.
- Autenticacion standalone configurable para operar sin Identity global.
- Contexto backend de usuario/tenant con tenant configurado y overrides por
  headers habilitables en desarrollo.
- Roles base `Operator`, `Supervisor`, `Quality`, `Maintenance` y
  `Administrator`.
- Operaciones sensibles protegidas por roles en MVC/API: cierre RCA,
  escalamiento 8D, cambios de estado de acciones, validacion/edicion sensible
  de evidencias, reemplazo/eliminacion de adjuntos y gestion interna de intake
  externo.
- Tabla `rca_audit_records` para auditoria inicial de operaciones sensibles.

## Prioridad Backend P0

El siguiente corte recomendado es endurecimiento de producto standalone:

- Autenticacion/autorizacion basica.
- Tenant real en lugar de tenant demo.
- Roles minimos: operador, supervisor, calidad, mantenimiento y administrador.
- Proteccion de operaciones sensibles: cierre, escalamiento 8D, revision o
  rechazo de intake, completar/cancelar acciones, validar evidencias y
  reemplazar/eliminar adjuntos.
- Auditoria fina para operaciones sensibles.
- Manejo consistente de errores API con `ApiResult`.
- Tests de dominio, aplicacion e integracion para flujos criticos.
- Smoke API + DB estable.
- Hardening de adjuntos: extension, tamano, path traversal, descarga
  controlada y preparacion para storage documental productivo.

## Limites

- No acoplar directamente con Gantt, SCADA, OEE, TPM, Andon, Identity ni AI
  Gateway.
- Mantener integracion mediante APIs, snapshots, eventos derivados y contratos
  versionados.
- No introducir modelos, tablas, endpoints ni reglas de negocio sin respaldo en
  documentos del repo o decision explicita.
- Mantener intake externo como acceso limitado por token, sin navegacion ni
  acceso al modulo completo.
- Mantener la IA como capacidad opt-in, con aprobacion humana para decisiones
  oficiales.

## Historial de Cambios Backend

### 2026-06-08

- Creado este documento como memoria viva del backend.
- Formalizada la regla de actualizacion cada vez que cambie el backend.
- Alineado el foco inmediato con P0: seguridad, tenant, auditoria, errores,
  tests, smoke e hardening de adjuntos.
- Agregada base de autenticacion/autorizacion standalone configurable.
- Reemplazado el `DemoTenantId` hardcodeado de MVC por tenant del contexto
  autenticado/configurado.
- Protegidas operaciones sensibles con roles base.
- Validado con `dotnet build IshikawaRca.sln` y `dotnet run --project
  tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`.
- Agregada entidad `RcaAuditRecord`, DbSet/mapping EF y migracion
  `AddRcaAuditRecords`.
- Registrada auditoria inicial para cierre RCA, escalamiento 8D, cambios de
  estado de acciones, actualizacion/reemplazo/eliminacion de evidencias y
  revision/rechazo/revocacion de intake externo.
- Validado nuevamente con build y tests livianos.
- Aplicada la migracion `20260608140016_AddRcaAuditRecords` en la base local
  usando `ISHIKAWA_RCA_CONNECTION`.
