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
- Siguiente tarea P2: implementar publicador/webhooks o endpoints operativos de
  outbox; el endpoint de eventos ya usa outbox + fallback derivado.

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

## Ultimo Cierre

- Fecha: 2026-06-11.
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
  feed derivado historico con deduplicacion por `id`.
- Commit sugerido: `feat(integration): merge outbox integration events`.
