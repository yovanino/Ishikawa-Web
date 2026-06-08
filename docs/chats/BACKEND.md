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

## Pendientes

- Relevar `Program.cs`, controllers API, servicios EF, entidades y DbContext.
- Definir siguiente paso de tenant productivo: resolver tenant desde Identity o
  proveedor corporativo cuando exista.
- Aplicar migracion `AddRcaAuditRecords` en la base local/productiva cuando se
  haga el siguiente corte DB.
- Extender consulta/UI/API de auditoria cuando se defina el consumidor.
- Normalizar errores backend.
- Agregar tests de politicas y servicios.
- Agregar smoke API + DB.
- Registrar validaciones en `docs/VALIDATION_LOG.md` cuando se ejecuten.

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

## Ultimo Cierre

- Fecha: 2026-06-08.
- Resumen: creada base de autenticacion/autorizacion standalone, tenant
  configurable para MVC/API, proteccion por roles y auditoria inicial para
  operaciones sensibles.
- Commit sugerido: `feat(backend): add standalone auth and audit context`.
