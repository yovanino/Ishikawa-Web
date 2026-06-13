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
- Base `HttpRcaAiGatewayClient` para AI Gateway real.
- Seleccion de runtime IA por `ConfiguredRcaAiGatewayClient`, con modo
  `Stub`/`Http` y fallback opcional al stub cuando falla el gateway HTTP.
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
- Canal live `GET /api/v1/integrations/rca/events/live` por Server-Sent Events
  sobre `RcaDomainEventDto` para timeline y estados sin acoplamiento directo.
- Endpoint protegido `GET /api/v1/integrations/rca/outbox/status` para
  observar conteos y timestamps operativos del outbox sin publicar ni reintentar
  eventos.
- Endpoint protegido `GET /api/v1/integrations/rca/outbox/dead-letter` para
  diagnosticar eventos outbox en `DeadLetter` sin reintentar ni cambiar estado.
- Endpoint protegido `POST /api/v1/integrations/rca/outbox/{id}/retry` para
  reprogramar eventos `Failed` o `DeadLetter` a `Pending` sin publicar.
- Endpoint protegido `POST /api/v1/integrations/rca/outbox/publish` para
  ejecutar publicacion manual del outbox.
- Configuracion `RcaIntegration` para futuro publicador outbox/webhooks, con
  webhooks deshabilitados por default y sin secretos versionados.
- Base de publicador `IRcaOutboxPublisher` / `RcaOutboxPublisher`, que por
  ahora retorna sin leer pendientes cuando no hay webhooks habilitados.
- Sender abstracto `IRcaWebhookSender` con flujo de publicacion que marca
  eventos como `Published` cuando todos los webhooks aplicables responden OK.
- Sender HTTP `RcaHttpWebhookSender` que publica `PayloadJson` por POST a la
  URL configurada y envia headers `X-RCA-Event-Id`, `X-RCA-Event-Type` y
  `X-RCA-Outbox-Id`.
- Firma HMAC SHA-256 en `X-RCA-Signature` cuando el webhook tiene `Secret`.
- Timeout configurable por `RcaIntegration:PublishTimeoutSeconds` para evitar
  que un destino lento bloquee la publicacion manual del outbox.
- Fallos de entrega webhook vuelven el evento a `Failed` con error resumido y
  `NextAttemptAt` inicial de 1 minuto.
- Eventos que alcanzan `RcaIntegration:MaxPublishAttempts` pasan a
  `DeadLetter`.

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

## Corte Backend P2

El corte de integracion operacional real queda cerrado para el alcance
standalone:

- Outbox transaccional persistido en `rca_outbox_events`.
- Captura outbox de los eventos RCA actuales, incluyendo intake externo.
- Feed `/api/v1/integrations/rca/events` combinado con outbox y fallback
  derivado historico.
- Webhooks configurables por `RcaIntegration`, apagados por default.
- Publicador manual protegido, firma HMAC, timeout configurable, fallo
  controlado, reintento manual y dead-letter.
- Canal live SSE para timeline/estados.
- Snapshots/correlacion para Gantt y facts idempotentes para Gateway/SCADA.

Los adapters directos contra Gantt, SCADA o Gateway no forman parte de este
repositorio. La integracion debe hacerse por APIs, snapshots, facts, eventos
live, outbox o webhooks versionados.

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
- Agregado `IRcaWebhookSender`, resultado `RcaWebhookSendResult` y
  `RcaHttpWebhookSender`. El publicador filtra webhooks por `EventTypes`,
  invoca el sender HTTP y marca el evento como `Published` si todos los
  destinos aplicables responden OK.
- `RcaHttpWebhookSender` firma el payload con HMAC SHA-256 en
  `X-RCA-Signature: sha256=<hex>` cuando `Secret` esta configurado.
- `RcaHttpWebhookSender` aplica `RcaIntegration:PublishTimeoutSeconds` por
  request y devuelve fallo controlado si el destino no responde a tiempo.
- El publicador marca `Failed` cuando algun destino aplicable falla, conserva
  el error resumido y programa `NextAttemptAt` con backoff inicial de 1 minuto.
- Agregado `MarkDeadLetterAsync`; cuando el intento actual alcanza
  `MaxPublishAttempts`, el publicador marca el evento como `DeadLetter` en vez
  de reprogramarlo.
- Agregado endpoint protegido `POST /api/v1/integrations/rca/outbox/publish`,
  que invoca `IRcaOutboxPublisher.PublishPendingAsync` y devuelve
  `RcaOutboxPublishResultDto`.
- Timeout configurado cubierto por prueba liviana y DI de infraestructura.
- Agregado endpoint live SSE `GET /api/v1/integrations/rca/events/live`, con
  polling configurable, cursor `since` y payload `RcaDomainEventDto`.

### 2026-06-12 - Base HTTP para AI Gateway P3

- Agregada opcion `RcaAiGatewayOptions` con `Mode`, `BaseUrl`,
  `TimeoutSeconds`, `ApiKey` y `UseFallbackOnFailure`.
- Agregado `HttpRcaAiGatewayClient` para publicar `RcaAiContextDto` a
  `/ai/rca/suggest-causes`, `/ai/rca/suggest-actions` y
  `/ai/rca/summarize`.
- El cliente HTTP aplica bearer opcional, timeout por request y fallos
  controlados para configuracion invalida, gateway no disponible o respuesta
  JSON vacia/invalida.
- Infraestructura ya bindea `AiGateway` a opciones.

### 2026-06-12 - Seleccion de modo IA y fallback P3

- Agregado `ConfiguredRcaAiGatewayClient` como wrapper de runtime para
  `IRcaAiGatewayClient`.
- `AiGateway:Mode = Stub` ejecuta `StubRcaAiGatewayClient` sin intentar HTTP.
- `AiGateway:Mode = Http` ejecuta `HttpRcaAiGatewayClient`.
- Si el cliente HTTP falla y `UseFallbackOnFailure = true`, el wrapper vuelve
  al stub y mantiene el modulo operativo.
- Si el cliente HTTP falla y `UseFallbackOnFailure = false`, se preserva la
  falla original del gateway.
- DI ahora registra `StubRcaAiGatewayClient` y `HttpRcaAiGatewayClient` como
  concretos, y expone `IRcaAiGatewayClient` mediante el wrapper configurado.

### 2026-06-12 - Hardening post-review de seleccion IA

- `ConfiguredRcaAiGatewayClient` ahora captura `HttpRequestException` del path
  HTTP, evitando que una falla de transporte escape del wrapper.
- Si `UseFallbackOnFailure = true`, una excepcion HTTP tambien cae al stub.
- Si `UseFallbackOnFailure = false`, la excepcion HTTP se normaliza a
  `AI_GATEWAY_UNAVAILABLE` con `ApiResult`.
- La DI de IA reutiliza un `HttpClient` compartido para
  `HttpRcaAiGatewayClient`, evitando recrearlo por scope sin agregar paquetes
  nuevos.

### 2026-06-13 - Recurrencia y borrador 8D P3

- Extendidos `IRcaAiAssistantService` e `IRcaAiGatewayClient` con
  `DetectRecurrenceAsync` y `GenerateEightDDraftAsync`.
- `RcaAiAssistantService` ahora reutiliza el armado de contexto RCA para
  deteccion de recurrencia y generacion de borrador 8D.
- `HttpRcaAiGatewayClient` publica a `/ai/rca/detect-recurrence` y
  `/ai/rca/generate-8d-draft`.
- `ConfiguredRcaAiGatewayClient` enruta ambos metodos por el mismo
  `ExecuteAsync` usado por causas, acciones y resumen.
- `StubRcaAiGatewayClient` agrega respuestas deterministicas para recurrencia y
  borrador 8D, ambas con `metadata.isFallback = true` y `metadata.generatedAt`
  fijo para no depender del reloj del servidor.
- `RcaAiController` expone `POST /api/v1/rca/incidents/{id}/ai/detect-recurrence`
  y `POST /api/v1/rca/incidents/{id}/ai/generate-8d-draft`.
- La suite liviana cubre el POST HTTP de recurrencia, la exposicion de ambos
  endpoints en controller y el comportamiento fallback/no mutacion del stub.

### 2026-06-13 - Hardening post-review de stub IA P3 Task 3

- `StubRcaAiGatewayClient` ahora usa una marca `generatedAt` fija para la
  metadata fallback.
- El resultado de recurrencia y borrador 8D en modo stub queda estable para
  llamadas identicas, incluyendo metadata.
- La suite liviana agrega una regresion para validar metadata deterministica en
  el stub.
