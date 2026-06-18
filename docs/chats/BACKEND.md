# Chat Backend

## Alcance

Endurecer el backend del modulo Ishikawa RCA para dejarlo listo como producto
standalone pilotable. El foco inicial es P0: seguridad minima, tenant real,
auditoria, tests, errores consistentes, hardening de adjuntos y base para
integraciones reales.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/backend.md`
- `docs/LOGICA_DE_FUNCIONAMIENTO.md`
- `docs/API_CONTRACTS.md`
- `docs/AI_INTEGRATION.md`
- `docs/EXTERNAL_CLAIM_INTAKE.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/BACKEND.md`

## Estado Actual

El backend ya tiene ASP.NET Core MVC/API por capas, EF Core + MySQL,
contratos API versionados, entidades RCA, evidencias, facts, intake externo,
wizard, cierre, escalamiento 8D, snapshots/feed de integracion e IA stub.

El siguiente corte recomendado es P0: endurecimiento tecnico del producto
standalone.

## Decisiones

- `docs/backend.md` queda como memoria viva del backend y debe actualizarse
  cada vez que cambie algo del backend.
- La bitacora tematica de continuidad es `docs/chats/BACKEND.md`.
- El backend debe mantener compatibilidad standalone y evitar acoplamiento
  directo con modulos futuros.
- Las integraciones deben mantenerse por API, snapshots, eventos derivados o
  contratos versionados.
- El frente P2 debe evolucionar el feed derivado hacia outbox/webhooks/canal
  live sin romper `RcaDomainEventDto` ni acoplar consumidores a tablas internas.
- Todo proceso local largo o en background debe tener timeout explicito y plan
  de apagado; no se deben dejar servidores `dotnet run`, watchers ni browsers
  automatizados vivos al cerrar una tarea.

## Cambios Realizados

- Creado `docs/backend.md`.
- Creada esta bitacora `docs/chats/BACKEND.md`.
- Actualizado el modelo operativo para incluir `docs/backend.md` en el frente
  Backend.
- Agregada autenticacion standalone configurable en `Program.cs`.
- Agregado contexto `ICurrentRcaUserContext` para resolver tenant y usuario
  actual.
- Reemplazado el tenant demo hardcodeado en `RcaController`.
- Protegidas operaciones sensibles MVC/API con roles base:
  `Supervisor`, `Quality`, `Maintenance` y `Administrator` segun operacion.
- Configurado `RcaSecurity` en `appsettings.json` y
  `appsettings.Development.json`.
- Agregada entidad `RcaAuditRecord` y tabla `rca_audit_records`.
- Generada migracion EF `AddRcaAuditRecords`.
- Registrada auditoria para operaciones sensibles: cierre, escalamiento 8D,
  cambios de estado de acciones, evidencias e intake externo interno.
- Endurecido storage local de evidencias con limite configurable
  `EvidenceStorage:MaxFileSizeMb` y validacion reforzada de rutas dentro del
  root.
- Agregadas pruebas livianas de storage de evidencias.
- Normalizadas validaciones automaticas y excepciones no controladas de API
  para devolver `ApiResult`.
- Normalizadas denegaciones 401/403 de API para devolver `ApiResult` con
  `AUTHENTICATION_REQUIRED` o `FORBIDDEN`.
- Actualizado smoke local API + DB para cubrir flujo critico P0 completo y
  endpoints protegidos por roles.
- Reforzado `scripts/smoke-test.ps1` para validar la descarga controlada del
  adjunto de evidencia comparando bytes, content-type y content-disposition.
- Agregado preflight `scripts/check-dotnet-sdk.ps1` y opcion `-Build` en
  `scripts/run-local-validation.ps1` para detectar SDK faltante antes de una
  validacion local completa.
- Agregado smoke repetible `scripts/smoke-api-auth-errors.ps1` para cubrir
  contrato 401/403 de endpoints API protegidos; `run-local-validation.ps1` lo
  ejecuta despues del smoke funcional DB.
- Agregado smoke repetible `scripts/smoke-api-model-validation.ps1` para cubrir
  `MODEL_VALIDATION_ERROR` y `correlationId` en errores de model binding API.
- Agregado smoke repetible `scripts/smoke-evidence-attachment-validation.ps1`
  para cubrir rechazo de adjuntos de evidencia con extension no permitida.
- Agregado endpoint protegido `GET /api/v1/rca/incidents/{id}/audit` para
  consultar registros de auditoria del RCA.
- Agregado smoke repetible `scripts/smoke-audit-records.ps1` para validar que
  operaciones sensibles escriben auditoria consultable.
- Agregado smoke repetible `scripts/smoke-external-facts.ps1` para cubrir
  ingestion de facts externos, idempotencia por `externalSourceSystem` +
  `externalEventId` y validacion de correlacion incompleta.
- Aclarada la guia local: runtime/smokes usan `ConnectionStrings__IshikawaRca`;
  `ISHIKAWA_RCA_CONNECTION` queda para EF design-time.
- Actualizado `docs/STATUS_AND_NEXT_STEPS.md` para reflejar el estado P0
  alcanzado y los pendientes inmediatos vigentes.
- Documentado el contrato de eventos de integracion RCA en
  `docs/INTEGRATION_EVENTS.md`.
- Actualizado `docs/API_CONTRACTS.md` para referenciar envelope, reglas de
  compatibilidad e instrucciones de consumo del feed derivado.
- Marcado en roadmap el primer avance P2: versionado y compatibilidad de
  eventos.
- Agregada cobertura liviana del contrato de eventos sobre
  `InMemoryRcaIncidentService`, validando envelope, correlacion externa, tipos
  documentados, claves `data` criticas y filtro `since`.
- Especificado el diseno P2 de outbox transaccional y webhooks en
  `docs/superpowers/specs/2026-06-11-p2-rca-outbox-webhooks-design.md`.
- Creado plan de implementacion para la base outbox en
  `docs/superpowers/plans/2026-06-11-p2-rca-outbox-base.md`.
- Agregado modelo de dominio inicial del outbox: `RcaOutboxEventStatus` y
  `RcaOutboxEvent`.
- Agregado mapping EF y migracion `AddRcaOutboxEvents` para
  `rca_outbox_events`.
- Agregado servicio base `IRcaOutboxService` / `EfRcaOutboxService` y registro
  DI en infraestructura.
- Agregada captura automatica inicial al outbox para `RcaIncidentCreated`,
  `RcaCorrectiveActionCompleted`, `RcaFactRecorded` y `RcaClosed`.
- Extendida captura outbox de `EfRcaIncidentService` a causas, acciones
  creadas, evidencias, wizard y escalamiento 8D.
- Agregada captura outbox en `EfRcaExternalIntakeService` para eventos de
  intake externo cliente/proveedor.
- Actualizado `ListIntegrationEventsAsync` para combinar eventos outbox y feed
  derivado, deduplicando por `id`.
- Agregado canal live por Server-Sent Events en
  `GET /api/v1/integrations/rca/events/live`, reutilizando
  `RcaDomainEventDto` para timeline y estados.
- Agregado endpoint protegido
  `GET /api/v1/integrations/rca/outbox/status` para exponer estado operativo
  del outbox con conteos por estado y timestamps de pendientes, reintentos,
  intentos y publicaciones.
- Agregada configuracion base `RcaIntegration` para futuro
  publicador/webhooks, con defaults seguros y webhooks deshabilitados por
  defecto.
- Agregado endpoint protegido
  `GET /api/v1/integrations/rca/outbox/dead-letter?take=` para diagnosticar
  eventos outbox en `DeadLetter` sin reintentar ni cambiar estado.
- Agregado endpoint protegido
  `POST /api/v1/integrations/rca/outbox/{id}/retry` para reprogramar eventos
  `Failed` o `DeadLetter` a `Pending` sin publicar.
- Agregada base `IRcaOutboxPublisher` / `RcaOutboxPublisher` con resultado
  `RcaOutboxPublishResultDto`. Por ahora, si no hay webhooks habilitados, el
  publicador retorna sin consultar pendientes ni modificar el outbox.
- Agregado `IRcaWebhookSender` / `RcaWebhookSendResult` y flujo de publicacion
  contra sender abstracto. El publicador filtra por `EventTypes` y marca
  eventos `Published` cuando todos los destinos aplicables responden OK.
- Agregado `RcaHttpWebhookSender`, que publica `PayloadJson` por POST a la URL
  configurada e incluye headers `X-RCA-Event-Id`, `X-RCA-Event-Type` y
  `X-RCA-Outbox-Id`.
- Agregada firma HMAC SHA-256 de webhooks en `X-RCA-Signature` cuando el
  webhook tiene `Secret`.
- Agregado timeout configurable por `RcaIntegration:PublishTimeoutSeconds` en
  `RcaHttpWebhookSender`, con fallo controlado cuando el destino HTTP no
  responde a tiempo.
- Agregado manejo inicial de fallo en publicador: si un webhook aplicable
  falla, el evento se marca `Failed`, guarda error resumido y queda con
  `NextAttemptAt` a 1 minuto.
- Agregado paso a `DeadLetter` cuando el intento actual alcanza
  `RcaIntegration:MaxPublishAttempts`.
- Agregado endpoint protegido `POST /api/v1/integrations/rca/outbox/publish`
  para disparar manualmente `IRcaOutboxPublisher`.
- Agregada la base P3 `RcaAiGatewayOptions` + `HttpRcaAiGatewayClient` para
  publicar contexto RCA al AI Gateway por HTTP JSON, con bearer opcional,
  timeout y fallos controlados.
- Infraestructura ya carga la seccion `AiGateway`, pero el runtime sigue usando
  `StubRcaAiGatewayClient`; la seleccion por modo/fallback queda para la
  Task 2 del plan P3.
- Hardening post-review para `HttpRcaAiGatewayClient`: ahora preserva prefijos
  de path en `AiGateway:BaseUrl`, encapsula timeout durante lectura del body
  como fallo controlado y agrega cobertura para configuracion invalida, HTTP no
  exitoso, JSON invalido y timeout de deserializacion.
- Agregado `ConfiguredRcaAiGatewayClient` para seleccionar `Mode = Stub` o
  `Mode = Http` segun `AiGateway`, con fallback opcional al stub cuando falla
  HTTP.
- Actualizado `DependencyInjection` para registrar `StubRcaAiGatewayClient` y
  `HttpRcaAiGatewayClient` como concretos y exponer `IRcaAiGatewayClient`
  mediante el wrapper configurado.
- Agregada cobertura liviana para fallback a stub, modo stub forzado y retorno
  de fallo HTTP cuando el fallback esta deshabilitado.
- Hardening post-review de la Task 2: el wrapper ahora captura
  `HttpRequestException` del path HTTP para hacer fallback a stub o devolver
  `AI_GATEWAY_UNAVAILABLE` sin dejar escapar excepciones de transporte.
- Agregada prueba DI que valida que `AddIshikawaRcaInfrastructure` resuelve
  `IRcaAiGatewayClient` como `ConfiguredRcaAiGatewayClient`.
- Ajustada la DI del cliente IA para reutilizar un `HttpClient` compartido en
  lugar de crear uno nuevo por scope, sin agregar paquetes.
- Cerrada la Task 3 P3 con contratos y endpoints de deteccion de recurrencia y
  borrador 8D.
- Agregados `RcaAiRecurrenceResultDto` y `RcaAiEightDDraftResultDto` en
  `Contracts`.
- Extendidos `IRcaAiAssistantService`, `IRcaAiGatewayClient`,
  `RcaAiAssistantService`, `ConfiguredRcaAiGatewayClient`,
  `HttpRcaAiGatewayClient`, `StubRcaAiGatewayClient` y `RcaAiController`.
- Agregada cobertura liviana para el POST HTTP a
  `/ai/rca/detect-recurrence`, la exposicion controller de
  `detect-recurrence` + `generate-8d-draft` y el comportamiento fallback/no
  mutacion del stub.

## Pendientes

- Definir siguiente paso de tenant productivo: resolver tenant desde Identity o
  proveedor corporativo cuando exista.
- Aplicar migracion `AddRcaAuditRecords` en ambientes no locales cuando se haga
  el siguiente corte DB/deploy.
- Extender UI/API de auditoria, reapertura gobernada y reportes cuando se
  defina el consumidor corporativo.
- Agregar suite formal ampliada de tests sobre la base liviana actual.
- Ampliar hardening de adjuntos con validacion de content-type/firma cuando se
  defina politica documental productiva.
- Mantener smoke API + DB en cada corte backend significativo.
- Registrar validaciones en `docs/VALIDATION_LOG.md` cuando se ejecuten.
- Elegir el siguiente incremento P2: outbox transaccional, webhooks
  configurables o canal live para timeline/estados.
- Implementar entidad/mapping/migracion `RcaOutboxEvent` como siguiente ajuste
  P2, conservando el feed derivado hasta igualar cobertura outbox.
- Ejecutar el plan outbox base por tareas, con commit al final de cada ajuste.
- Siguiente tarea P2: agregar smoke manual de webhook y revisar cierre P2;
  el endpoint de eventos ya usa outbox + fallback derivado, el endpoint de
  status ya cubre observabilidad, dead-letter ya tiene consulta, el retry
  manual ya existe, el canal live SSE ya existe y la entrega HTTP basica con
  firma HMAC, timeout configurado, fallo inicial y dead-letter por maximos
  intentos ya esta registrada. El endpoint publish manual ya existe.
- P2 queda cerrado para el alcance standalone: outbox transaccional, webhooks,
  endpoint publish, retry/dead-letter/status, canal live SSE, contratos de
  eventos, snapshots Gantt y facts Gateway/SCADA. Los adapters a sistemas
  externos quedan fuera del repo por limite de modulo.
- Siguiente tramo P3 despues de Task 3: UI de aprobacion humana de sugerencias
  IA y auditoria de aceptacion/rechazo.

## Riesgos

- Avanzar integraciones reales sin outbox o auditoria puede dejar trazabilidad
  debil.
- Agregar seguridad sin una decision clara de tenant/roles puede generar
  compatibilidad fragil.
- Cambiar contratos sin actualizar `docs/API_CONTRACTS.md` puede romper
  consumidores externos.

## Validaciones

- Revision documental manual.
- `dotnet build IshikawaRca.sln`: correcto, 0 errores. Quedaron warnings
  `NU1900` por no poder consultar vulnerabilidades en `https://api.nuget.org`
  desde el entorno restringido.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  correcto, sin errores.
- `dotnet ef migrations add AddRcaAuditRecords --project
  src/IshikawaRca.Infrastructure/IshikawaRca.Infrastructure.csproj
  --startup-project src/IshikawaRca.Web/IshikawaRca.Web.csproj`: correcto.
- `dotnet ef database update --project
  src/IshikawaRca.Infrastructure/IshikawaRca.Infrastructure.csproj
  --startup-project src/IshikawaRca.Web/IshikawaRca.Web.csproj`: aplicado
  correctamente usando `ISHIKAWA_RCA_CONNECTION` local.
- `dotnet build IshikawaRca.sln`: correcto, 0 errores; warnings `NU1900` por
  red restringida contra NuGet.
- `dotnet run --project tests/IshikawaRca.Tests/IshikawaRca.Tests.csproj`:
  correcto, incluye pruebas de hardening de adjuntos.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; smoke API + DB crea, valida, escala y cierra RCA.
- Para el ajuste 401/403, `dotnet build IshikawaRca.sln /m:1` y fallback con
  MSBuild quedaron bloqueados porque el entorno local no tiene SDK .NET
  registrado; `dotnet --info` lista runtimes, pero no SDKs. Se completo
  revision estatica de diff/referencias.
- Instalado `Microsoft.DotNet.SDK.10` version `10.0.301` mediante `winget` para
  satisfacer `global.json` `10.0.300` por patch compatible.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\check-dotnet-sdk.ps1`: falla rapido con mensaje claro porque no hay
  SDKs registrados y `global.json` pide `10.0.300`.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 5 -RequestTimeoutSeconds 5 -ShutdownTimeoutSeconds 5`:
  falla rapido por el mismo preflight y no deja proceso web vivo.
- `dotnet build IshikawaRca.sln /m:1`: correcto, 0 warnings y 0 errores.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  correcto.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto usando `ConnectionStrings__IshikawaRca` local con
  `AllowPublicKeyRetrieval=True`.
- Validado contrato 401/403 en
  `POST /api/v1/rca/incidents/{id}/close`: rol `Operator` devuelve
  `FORBIDDEN`; tenant invalido devuelve `AUTHENTICATION_REQUIRED`.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye smoke funcional API + DB y smoke API de errores
  401/403.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye smoke de facts externos con idempotencia por
  `externalSourceSystem` + `externalEventId` y rechazo de correlacion
  incompleta.
- `docs/LOCAL_OPERATIONS.md` actualizado para distinguir la variable runtime
  `ConnectionStrings__IshikawaRca` de `ISHIKAWA_RCA_CONNECTION` para EF
  design-time.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye smoke API de model validation con
  `MODEL_VALIDATION_ERROR` y `correlationId`.
- `dotnet build IshikawaRca.sln /m:1`: correcto, 0 warnings y 0 errores,
  despues de repetir secuencialmente por una carrera transitoria causada por
  ejecutar build/tests en paralelo.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  correcto.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye smoke de adjuntos de evidencia con rechazo
  `INVALID_ATTACHMENT`.
- `dotnet build IshikawaRca.sln /m:1`: correcto, 0 warnings y 0 errores,
  para el ajuste de descarga controlada de adjuntos.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  correcto para el ajuste de descarga controlada de adjuntos.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye descarga controlada con bytes, content-type y
  content-disposition.
- `dotnet build IshikawaRca.sln /m:1`: correcto, 0 warnings y 0 errores, para
  el ajuste de consulta de auditoria.
- `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`:
  correcto; incluye auditoria in-memory de cambio de estado de acciones.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
  .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025
  -StartupTimeoutSeconds 25 -RequestTimeoutSeconds 15 -ShutdownTimeoutSeconds
  10`: correcto; incluye `smoke-audit-records.ps1`.
- `docs/STATUS_AND_NEXT_STEPS.md` alineado con P0 actual: auth/tenant/roles,
  auditoria inicial, errores API, hardening inicial de adjuntos y smokes.
- Para el ajuste P2 de compatibilidad de eventos, se validan build, tests
  livianos y `git diff --check` antes del commit.
- Para la cobertura P2 de eventos, `dotnet build IshikawaRca.sln /m:1` y
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  pasan con 0 errores.
- Para la spec P2 outbox/webhooks, validacion documental y `git diff --check`.
- Para el plan P2 outbox base, validacion documental y `git diff --check`.
- Para el modelo de dominio outbox, primero se confirmo falla de build por
  tipos inexistentes y luego pasaron build, tests livianos y `git diff --check`.
- Para mapping/migracion outbox, `dotnet build IshikawaRca.sln /m:1`,
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `git diff --check` pasan. La primera migracion generada con `--no-build`
  quedo vacia por assembly desactualizado; se removio y regenero despues de
  compilar.
- Para servicio base outbox, `dotnet build IshikawaRca.sln /m:1`,
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `git diff --check` pasan.
- Para captura inicial de eventos outbox, `dotnet build IshikawaRca.sln /m:1`,
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `git diff --check` pasan.
- Para ampliar cobertura en `EfRcaIncidentService`, `dotnet build
  IshikawaRca.sln /m:1`, `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `git diff --check`
  pasan.
- Para captura outbox de intake externo, `dotnet build IshikawaRca.sln /m:1`,
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `git diff --check` pasan.
- Para mezcla outbox + feed derivado, el primer intento paralelo de
  build/tests fallo por carrera transitoria de artefactos; rerun en serie de
  `dotnet build IshikawaRca.sln /m:1`, `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `git diff --check`
  pasa.
- Para endpoint de status del outbox, `dotnet build IshikawaRca.sln /m:1` y
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  pasan en serie. `git diff --check` queda pendiente antes del commit.
- Para configuracion base de webhooks, `dotnet build IshikawaRca.sln /m:1`
  pasa despues de cambiar a binding manual sin paquetes nuevos. Tests y
  `git diff --check` quedan pendientes antes del commit.
- Para consulta dead-letter del outbox, `dotnet build IshikawaRca.sln /m:1`
  pasa con 0 warnings y 0 errores. Tests y `git diff --check` quedan
  pendientes antes del commit.
- Para retry manual del outbox, `dotnet build IshikawaRca.sln /m:1` pasa con
  0 warnings y 0 errores. Tests y `git diff --check` quedan pendientes antes
  del commit.
- Para base del publicador outbox, se agrego primero una prueba RED que fallaba
  por `RcaOutboxPublisher` inexistente. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para sender abstracto del publicador, se agrego primero una prueba RED que
  fallaba por `IRcaWebhookSender` / `RcaWebhookSendResult` inexistentes. Luego
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `dotnet build IshikawaRca.sln /m:1` pasan en serie.
- Para sender HTTP real, se agrego primero una prueba RED que fallaba por
  `RcaHttpWebhookSender` inexistente. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para firma HMAC de webhooks, se agrego primero una prueba RED que fallaba por
  ausencia de `X-RCA-Signature`. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para fallo inicial del publicador, se agrego primero una prueba RED que
  esperaba `MarkFailedAsync`. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para dead-letter por maximos intentos, se agrego primero una prueba RED que
  esperaba `MarkDeadLetterAsync`. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para endpoint manual publish, se agrego primero una prueba RED que fallaba
  por constructor/action faltantes en `RcaIntegrationsController`. Luego
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y
  `dotnet build IshikawaRca.sln /m:1` pasan en serie.
- Para timeout configurado del sender HTTP, se agrego primero una prueba RED
  que fallaba porque `RcaHttpWebhookSender` no aceptaba opciones de
  `RcaIntegration`. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para canal live SSE, se agrego primero una prueba RED que fallaba porque
  `RcaIntegrationsController.StreamEvents` no existia. Luego `dotnet run
  --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` y `dotnet build
  IshikawaRca.sln /m:1` pasan en serie.
- Para la base HTTP del AI Gateway P3, se agrego primero una prueba RED que
  fallaba por `HttpRcaAiGatewayClient` y `RcaAiGatewayOptions` inexistentes.
  Luego `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`,
  `dotnet build IshikawaRca.sln /m:1` y `git diff --check` pasan en serie.
- Para el hardening post-review del cliente HTTP IA, se agregaron pruebas RED
  para preservacion de prefijo en `BaseUrl`, `AI_GATEWAY_CONFIGURATION_INVALID`,
  `AI_GATEWAY_UNAVAILABLE`, `AI_GATEWAY_INVALID_RESPONSE` y timeout durante
  lectura del body. El primer intento del harness de timeout fallo por una
  simulacion no soportada; corregido el harness, `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj` pasa y quedan pendientes
  build + `git diff --check` finales antes del commit.
- Para la Task 2 de seleccion de modo/fallback IA, se agregaron pruebas RED
  que fallaban por `ConfiguredRcaAiGatewayClient` inexistente. Luego
  `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`,
  `dotnet build IshikawaRca.sln /m:1` y `git diff --check` deben pasar en
  serie antes del commit.
- Para el hardening post-review de la Task 2, se agregaron pruebas RED para
  fallback ante `HttpRequestException`, devolucion controlada
  `AI_GATEWAY_UNAVAILABLE` sin fallback y resolucion DI real de
  `IRcaAiGatewayClient`. Luego `dotnet run --project
  tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`, `dotnet build
  IshikawaRca.sln /m:1` y `git diff --check` deben pasar en serie antes del
  commit.
- Para la Task 3 P3, el estado heredado ya traia la base de codigo verde al
  inspeccionarlo; se completo la cobertura faltante del stub y el cierre
  documental. La validacion final requerida queda registrada en
  `docs/VALIDATION_LOG.md`.

## Ultimo Cierre

- Fecha: 2026-06-12.
- Resumen: iniciado P2 de integracion operacional real con documentacion formal
  del feed de eventos RCA, envelope `RcaDomainEventDto`, compatibilidad
  `/api/v1`, deduplicacion por `id`, polling por `occurredAt` y evolucion
  esperada hacia outbox/webhooks/SignalR sin acoplar consumidores externos al
  modelo interno. Agregada prueba liviana que protege ese contrato antes de
  avanzar a outbox o webhooks. Definida spec tecnica para implementar outbox
  primero y webhooks despues. Creado plan ejecutable para la base outbox y
  agregado el modelo de dominio inicial. Agregado mapping EF y migracion de
  tabla outbox. Agregado servicio base outbox para enqueue idempotente y
  cambios de estado de entrega. Agregada captura outbox inicial de eventos de
  alto valor sin reemplazar el feed derivado. Extendida la captura del servicio
  RCA principal a causas, acciones creadas, evidencias, wizard y 8D. Agregada
  captura outbox de intake externo. El endpoint de eventos combina outbox y
  feed derivado historico con deduplicacion por `id`. Agregado endpoint
  protegido de status del outbox para observabilidad operacional. Agregada
  configuracion base `RcaIntegration` para futuros webhooks, apagados por
  default. Agregada consulta protegida de dead-letter para diagnostico
  operativo. Agregado retry manual protegido para reprogramar eventos
  `Failed`/`DeadLetter`. Agregada base del publicador outbox con comportamiento
  seguro cuando no hay webhooks habilitados. Agregado sender abstracto y flujo
  para marcar eventos como publicados cuando la entrega abstracta tiene exito.
  Agregado sender HTTP real para POST de payload outbox y firma HMAC cuando hay
  secreto configurado. Agregado fallo inicial del publicador con `Failed` y
  `NextAttemptAt` a 1 minuto. Agregado paso a `DeadLetter` por maximos
  intentos. Agregado endpoint manual publish del outbox. Agregado timeout
  configurado para webhooks y canal live SSE para timeline/estados. Cerrada la
  matriz P2 standalone con outbox, webhooks, contratos broker-ready, Gantt por
  snapshots/correlacion, Gateway/SCADA por facts idempotentes y estados
  operativos para consumidores externos. Iniciada Task 1 de P3 con
  `RcaAiGatewayOptions`, `HttpRcaAiGatewayClient`, prueba RED/GREEN del POST de
  contexto RCA y binding de `AiGateway`, manteniendo stub como runtime actual
  hasta la seleccion por modo/fallback de la Task 2. Luego se endurece el
  cliente por review: preserva prefijos del `BaseUrl`, controla timeout durante
  deserializacion y amplía cobertura de fallos.
- Commit sugerido: `fix(ai): harden HTTP AI gateway client`.
- Actualizacion Task 2 P3: agregado `ConfiguredRcaAiGatewayClient`, activada la
  seleccion por `AiGateway:Mode = Stub/Http`, definido fallback controlado por
  `UseFallbackOnFailure` y agregadas pruebas livianas para fallback a stub,
  modo stub directo y devolucion de fallo HTTP sin fallback.
- Commit sugerido: `feat(ai): select AI gateway mode with fallback`.
- Actualizacion Task 3 P3: cerrados los contratos y endpoints de
  `detect-recurrence` y `generate-8d-draft`, con DTOs nuevos, soporte completo
  en servicio/gateway/controller, POST HTTP a AI Gateway para recurrencia y
  cobertura liviana adicional del stub. Commit requerido:
  `feat(ai): add recurrence and 8D draft suggestions`.
- Hardening post-review Task 3 P3: `RcaAiController` ahora distingue
  `RCA_NOT_FOUND` (`404`) de fallas `AI_GATEWAY_*` (`503`) en todos los
  endpoints de asistencia IA. Se agrego prueba RED/GREEN para gateway caido y
  RCA inexistente, con validacion en serie verde.
- Inicio y cierre tecnico Task 4 P3: agregada base persistente auditable de
  sugerencias IA con `RcaAiSuggestion`, enums de tipo/estado, mapping EF,
  migracion `AddRcaAiSuggestions` y prueba RED/GREEN de defaults de dominio.
- Task 5 P3: agregado `IRcaAiSuggestionStore`, `EfRcaAiSuggestionStore` y
  `RcaAiSuggestionDto`; `RcaAiAssistantService` ahora persiste sugerencias
  pendientes tras respuestas exitosas del gateway para causas, acciones,
  resumen, recurrencia y 8D. Se valido con prueba RED/GREEN de persistencia de
  causas pendientes.
- Hardening post-review Task 5 P3: el store IA pasa a guardado batch con un
  unico `SaveChanges`, recorta strings a limites de esquema, calcula
  `GatewayCorrelationId` estable, filtra duplicados, agrega indice unico
  `TenantId + GatewayCorrelationId`, lista por tenant/incidente y rechaza status
  invalidos con `AI_SUGGESTION_STATUS_INVALID`.
- Re-review Task 5 P3: la migracion del indice unico backfillea
  `GatewayCorrelationId` vacios antes de crear el indice, y el parser de status
  rechaza numericos fuera del enum como `999`.
- Task 6 P3: agregado gobierno de revision humana para sugerencias IA. La API
  lista sugerencias, acepta/rechaza con roles de calidad, aplica causas/acciones
  oficiales solo tras aceptacion y audita `AiSuggestionAccepted` /
  `AiSuggestionRejected`.
- Hardening post-review Task 6 P3: toda la superficie IA requiere autenticacion,
  el usuario auditado sale del contexto autenticado, accept/reject corre con
  transaccion de revision y claim atomico `Pending -> Accepted` antes de mutar
  entidades oficiales, los tipos Summary/Recurrence/8D pueden aceptarse como
  decision auditada sin mutacion oficial, y causa sin rama devuelve
  `AI_SUGGESTION_BRANCH_REQUIRED`.
- Task 7 P3: el detalle MVC del RCA carga sugerencias IA pendientes y agrega
  panel de revision humana para aceptar o rechazar. Las acciones MVC requieren
  antiforgery y rol de gobernanza de calidad; aceptar causas exige seleccionar
  rama destino y delega en el workflow auditado existente.
- Hardening post-review Task 7 P3: el detalle MVC solo consulta y renderiza
  sugerencias IA pendientes cuando el usuario actual tiene rol de gobernanza de
  calidad.
- Task 8 P3: cerrado documentalmente el alcance standalone de IA gobernada.
  Queda documentado que la politica IA especifica por tenant es post-P3 hasta
  Identity/tenant corporativo real.
- Inicio P4.1: agregado modelo de dominio `RcaClosureDocument` y enum
  `RcaClosureDocumentStatus` para versionado futuro de documentos de cierre RCA.
  Validado con RED/GREEN de suite liviana y build serial.
- P4.1 Task 2: agregado mapping EF, DbSet y migracion
  `AddRcaClosureDocuments` para `rca_closure_documents`, con version unico por
  tenant/RCA e indices de consulta documental.
- P4.1 Task 3a: agregados contratos e interfaz de aplicacion para registrar,
  listar, aprobar y rechazar documentos de cierre RCA.
- P4.1 Task 3b: agregado `EfRcaClosureDocumentService`, con versionado
  incremental por tenant/RCA, validacion de RCA cerrado y auditoria de
  generacion/aprobacion/rechazo.
- P4.1 Task 3c: agregado storage documental local para PDFs de cierre con
  limite configurable, SHA-256 y resolucion segura dentro del root configurado.
- P4.1 Task 4a: `ExportPdf` ahora guarda el PDF generado en storage documental
  y registra una version de cierre mediante `IRcaClosureDocumentService`; ante
  falla de registro elimina el archivo recien generado.
- P4.1 Task 4b: agregado `RcaDocumentsController` para listar, descargar,
  aprobar y rechazar documentos de cierre por API `/api/v1`, usando el usuario
  autenticado como revisor.
