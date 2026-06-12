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
- Contratos documentales de integracion operativa, eventos y compatibilidad
  `/api/v1`.

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
- Respuestas API consistentes con `ApiResult` para validaciones, errores 500 y
  fallas 401/403 de autenticacion/autorizacion.
- Tabla `rca_audit_records` para auditoria inicial de operaciones sensibles.
- Endpoint protegido `GET /api/v1/rca/incidents/{id}/audit` para consultar la
  auditoria persistida de un RCA.
- Contrato documentado de eventos de integracion en
  `docs/INTEGRATION_EVENTS.md`, basado en `RcaDomainEventDto`.
- Cobertura liviana de compatibilidad para eventos de integracion en
  `tests/IshikawaRca.Tests`.
- Diseno tecnico P2 para outbox transaccional y webhooks documentado en
  `docs/superpowers/specs/2026-06-11-p2-rca-outbox-webhooks-design.md`.
- Modelo de dominio inicial para outbox: `RcaOutboxEvent` y
  `RcaOutboxEventStatus`.
- Mapping EF y migracion `AddRcaOutboxEvents` para la tabla
  `rca_outbox_events`.
- Servicio base `IRcaOutboxService` / `EfRcaOutboxService` para persistir y
  administrar eventos outbox.
- Captura automatica inicial hacia outbox para eventos de alto valor:
  `RcaIncidentCreated`, `RcaCorrectiveActionCompleted`, `RcaFactRecorded` y
  `RcaClosed`.
- Cobertura outbox adicional en `EfRcaIncidentService` para causas, acciones
  creadas, evidencias, wizard y escalamiento 8D.
- Captura outbox para estados de intake externo cliente/proveedor en
  `EfRcaExternalIntakeService`.
- El feed de eventos de integracion combina outbox y feed derivado con
  deduplicacion por `id`.
- Endpoint protegido `GET /api/v1/integrations/rca/outbox/status` para
  observar conteos y timestamps operativos del outbox sin publicar ni reintentar
  eventos.
- Endpoint protegido `GET /api/v1/integrations/rca/outbox/dead-letter` para
  diagnosticar eventos outbox en `DeadLetter` sin reintentar ni cambiar estado.
- Endpoint protegido `POST /api/v1/integrations/rca/outbox/{id}/retry` para
  reprogramar eventos `Failed` o `DeadLetter` a `Pending` sin publicar.
- Configuracion `RcaIntegration` para futuro publicador outbox/webhooks, con
  webhooks deshabilitados por default y sin secretos versionados.
- Base de publicador `IRcaOutboxPublisher` / `RcaOutboxPublisher`, que por
  ahora retorna sin leer pendientes cuando no hay webhooks habilitados.

## Corte Backend P0

El corte de endurecimiento de producto standalone queda cerrado como base
pilotable:

- Autenticacion/autorizacion basica standalone.
- Tenant configurable en lugar de tenant demo.
- Roles minimos: operador, supervisor, calidad, mantenimiento y administrador.
- Proteccion por rol de operaciones sensibles: cierre, escalamiento 8D, revision o
  rechazo de intake, completar/cancelar acciones, validar evidencias y
  reemplazar/eliminar adjuntos.
- Auditoria persistida y consulta protegida para operaciones sensibles.
- Manejo consistente de errores API con `ApiResult`.
- Tests base de dominio/aplicacion e integracion por smokes para flujos
  criticos.
- Smoke API + DB estable.
- Hardening de adjuntos: extension, tamano, path traversal, descarga
  controlada y preparacion para storage documental productivo.

Quedan como post-P0: Identity global, tenant corporativo multitenant, permisos
productivos finos, reapertura gobernada, reportes corporativos de auditoria,
tests MVC/UI, CI/CD y storage documental productivo.

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

### 2026-06-08 - Hardening de adjuntos

- `EvidenceStorage:MaxFileSizeMb` permite configurar el limite de adjuntos,
  manteniendo 100 MB como default.
- La resolucion y eliminacion de adjuntos ahora validan pertenencia al root con
  `Path.GetRelativePath`, evitando falsos positivos por prefijos de ruta.
- Agregadas pruebas livianas para limite configurable y rechazo de storage keys
  inseguras.
- Validado con `dotnet build IshikawaRca.sln` y `dotnet run --project
  tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`.

### 2026-06-08 - Errores API consistentes

- Las validaciones automaticas de modelo en API devuelven `ApiResult<object>`
  con codigo `MODEL_VALIDATION_ERROR`.
- Las excepciones no controladas bajo `/api` devuelven HTTP 500 con
  `UNHANDLED_API_ERROR` y `correlationId`, sin exponer detalles internos al
  consumidor.
- Las denegaciones de autenticacion/autorizacion bajo `/api` devuelven HTTP 401
  `AUTHENTICATION_REQUIRED` o HTTP 403 `FORBIDDEN` con `ApiResult<object>` y
  `correlationId`.

### 2026-06-08 - Smoke API + DB P0

- `scripts/smoke-test.ps1` ahora envia headers standalone de tenant, usuario y
  roles para cubrir endpoints protegidos.
- El smoke cubre cierre critico real: causa raiz, subcausa, evidencia validada,
  adjunto validado, accion correctiva, accion preventiva de recurrencia,
  escalamiento 8D, cierre formal, wizard cerrado, snapshot, eventos e IA stub.
- La descarga controlada del adjunto se valida dentro del smoke comparando
  bytes contra el archivo subido, `Content-Type` y `Content-Disposition`.
- `scripts/start-web.ps1` arranca correctamente cuando el path del repo contiene
  espacios y guarda logs de stdout/stderr ante fallos de startup.
- `scripts/check-dotnet-sdk.ps1` detecta rapido si falta el SDK requerido por
  `global.json` y acepta patches compatibles del mismo feature band;
  `run-local-validation.ps1 -Build` fuerza esa validacion antes de
  compilar/levantar la app.
- `scripts/smoke-api-auth-errors.ps1` valida el contrato API de errores 401/403
  para operaciones protegidas, y queda incluido en `run-local-validation.ps1`.
- `scripts/smoke-api-model-validation.ps1` valida el contrato API de errores de
  model binding con `MODEL_VALIDATION_ERROR` y `correlationId`.
- `scripts/smoke-evidence-attachment-validation.ps1` valida rechazo API de
  adjuntos de evidencia con extension no permitida.
- `scripts/smoke-audit-records.ps1` valida la consulta protegida de auditoria
  del incidente y queda incluido en `run-local-validation.ps1`.
- `scripts/smoke-external-facts.ps1` valida ingestion de facts externos por API,
  idempotencia por sistema/evento y rechazo de correlacion incompleta.
- Validado con `run-local-validation.ps1` contra DB local.

### 2026-06-11 - Compatibilidad de eventos P2

- Iniciado el frente P2 de integracion operacional real con documentacion del
  feed `GET /api/v1/integrations/rca/events`.
- Documentado `RcaDomainEventDto` como envelope estable para consumidores
  externos.
- Formalizadas reglas de compatibilidad `/api/v1`: cambios aditivos en `data`,
  ignorar claves/eventos desconocidos, deduplicacion por `id` y avance de
  polling por `occurredAt`.
- Registrada la evolucion esperada hacia outbox transaccional, webhooks,
  SignalR o broker preservando compatibilidad standalone.
- Agregada prueba liviana sobre `InMemoryRcaIncidentService` que valida tipos
  de evento documentados, envelope, correlacion externa, claves `data`
  criticas y filtro incremental `since`.
- Definido diseno P2 recomendado: `RcaOutboxEvent` como tabla transaccional,
  deduplicacion por `TenantId + EventId`, estados de publicacion, backoff,
  dead-letter y webhooks deshabilitados por default.
- Agregados `RcaOutboxEventStatus` (`Pending`, `Publishing`, `Published`,
  `Failed`, `DeadLetter`) y entidad `RcaOutboxEvent` con envelope,
  correlacion externa, payload JSON y campos de intento/publicacion.
- Agregada prueba liviana de defaults de dominio del outbox.
- Agregado `DbSet<RcaOutboxEvents>`, mapping EF e indices:
  `TenantId + EventId` unico, `TenantId + Status + NextAttemptAt`,
  `TenantId + IncidentId + OccurredAt` y `TenantId + EventType + OccurredAt`.
- Generada migracion `20260611123836_AddRcaOutboxEvents` para crear
  `rca_outbox_events`.
- Agregado `IRcaOutboxService` con operaciones de enqueue idempotente, listado
  de pendientes, marcado publicado y marcado fallido.
- Agregado `EfRcaOutboxService`, que serializa `RcaDomainEventDto`, deduplica
  por `TenantId + EventId`, lista eventos `Pending`/`Failed` elegibles y
  registra estado de entrega/reintento.
- Registrado el servicio outbox en `AddIshikawaRcaInfrastructure`.
- `EfRcaIncidentService` ahora agrega eventos outbox en la misma unidad de
  persistencia para creacion de RCA, accion completada, fact registrado y
  cierre RCA.
- Extendida la captura outbox del servicio RCA principal para
  `RcaCauseCreated`, `RcaRootCauseSelected`, `RcaCorrectiveActionCreated`,
  `RcaEvidenceAttached`, `RcaWizardStepCompleted` y `RcaEscalatedTo8D`.
- Agregada captura outbox de intake externo para `RcaExternalIntakeCreated`,
  `RcaExternalIntakeOpened`, `RcaExternalIntakeSubmitted`,
  `RcaExternalIntakeReviewed`, `RcaExternalIntakeRejected`,
  `RcaExternalIntakeRevoked` y `RcaExternalIntakeExpired`.
- El endpoint `/api/v1/integrations/rca/events` ahora lee eventos outbox
  persistidos y los combina con el feed derivado historico. Si un evento existe
  en ambas fuentes, se conserva una sola entrada por `id`, priorizando el
  payload del outbox.
- Agregado endpoint protegido
  `GET /api/v1/integrations/rca/outbox/status`, que devuelve total, conteos por
  estado y marcas temporales de pendientes, reintentos, intentos y publicaciones
  para observabilidad operacional del outbox.
- Agregadas opciones `RcaIntegrationOptions` / `RcaWebhookOptions`, binding
  manual en infraestructura y defaults seguros en `appsettings.json`:
  webhooks vacios, batch 50, 5 intentos maximos y timeout de 5 segundos.
- Agregado DTO `RcaOutboxEventDto`, consulta `ListDeadLettersAsync` y endpoint
  protegido `GET /api/v1/integrations/rca/outbox/dead-letter?take=` para
  inspeccionar eventos en dead-letter con limite acotado entre 1 y 500.
- Agregado `RetryRcaOutboxEventRequest`, servicio `ScheduleRetryAsync` y
  endpoint protegido `POST /api/v1/integrations/rca/outbox/{id}/retry`, que
  reprograma eventos `Failed`/`DeadLetter` a `Pending` y responde errores
  `OUTBOX_EVENT_NOT_FOUND` u `OUTBOX_EVENT_NOT_RETRYABLE`.
- Agregada base `IRcaOutboxPublisher` / `RcaOutboxPublisher` con resultado
  `RcaOutboxPublishResultDto`. El primer comportamiento validado es
  standalone-safe: si no hay webhooks habilitados, no lee eventos pendientes ni
  modifica el outbox.
- El outbox todavia no envia HTTP real, webhooks ni broker/event bus.
