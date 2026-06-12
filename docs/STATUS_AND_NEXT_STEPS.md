# Estado y Pendientes

Fecha de corte: 2026-06-04.

## Cerrado

1. Base modular del repo
   - Solucion ASP.NET Core MVC.
   - Proyectos separados: Web, Domain, Contracts, Application, Infrastructure.
   - Documentacion de alcance, limites, contratos, AI y roadmap.

2. Persistencia
   - EF Core + MySQL.
   - `RcaDbContext`.
   - Migracion inicial.
   - Servicio EF para incidentes, ramas, causas y acciones.

3. Flujo RCA funcional
   - Alta/listado/detalle de incidentes.
   - Canvas Ishikawa inicial.
   - Timeline visual inicial en detalle RCA.
   - Wizard RCA basico con etapas Problema, Causas, Evidencias, Acciones, Validacion y Cierre.
   - Carga de causas con puntajes.
   - Carga de subcausas vinculadas a una causa padre.
   - Marcado de causa raiz.
   - Acciones correctivas.
   - Validacion/cierre de acciones correctivas con nota obligatoria y validador opcional.
   - Cierre formal de RCA con causa raiz, acciones cerradas y resumen obligatorio.
   - Escalamiento formal a 8D con fecha, usuario y motivo.
   - Registro inicial de evidencias RCA con tipo, fuente, resumen, URI/referencia y relacion opcional a causa o intake externo.
   - Adjuntos reales de evidencia: documentos, imagenes, PDF y videos con metadata, descarga controlada y SHA-256.
   - Miniaturas/previews compactos para imagenes, video, PDF, Office, texto/CSV/JSON/XML y archivo generico.
   - Acciones de evidencia: editar metadata, reemplazar adjunto y eliminar evidencia.
   - Evidencias reforzadas con tags, fuente detallada, estado de validacion, validador, fecha y notas.
   - Wizard guiado con checklist, porcentaje, bloqueos, metricas por etapa y endpoint de progreso.
   - Exportacion PDF del RCA con cierre, causa raiz, acciones, manifiesto de evidencias, validacion, SHA-256 y links controlados.
   - Linea de hechos manual RCA con fecha/hora, tipo, fuente, clasificacion industrial, causa/evidencia/accion/intake opcional, API y exportacion PDF.
   - Hechos RCA por API con correlacion externa, URI de registro e idempotencia por sistema/evento.
   - Timeline unificado en detalle RCA para hechos, evidencias, acciones, wizard, eventos externos y contexto industrial.
   - Acciones clasificadas por tipo correctiva/preventiva/preventiva de recurrencia y por ambito causa raiz/FUGA.
   - Politica de resolucion que exige accion preventiva de recurrencia para causa raiz y set completo si existe FUGA.

4. Integracion externa
   - Endpoints versionados `/api/v1`.
   - Snapshots para Gantt, Gateway, OEE, Andon o app global.
   - `wizardStep` disponible en DTO de incidente y snapshot de integracion.
   - `wizard/progress` disponible para app global, AI Gateway o dashboards externos.
   - Feed derivado de eventos RCA.
   - Conteo de evidencia en snapshots de integracion.
   - Eventos de accion correctiva creada y completada para integracion.
   - Evento de RCA escalado a 8D para integracion.
   - Evento de RCA cerrado para integracion.
   - Evento de etapa wizard RCA completada para integracion.
   - Actor de reclamo: area interna, cliente o proveedor.
   - Intake link seguro MVP para cliente/proveedor con token hasheado.
   - Revision interna de intake externo con importacion a causa Ishikawa y accion correctiva.
   - Rechazo formal de intake externo con motivo obligatorio.
   - Eventos de integracion para intake externo creado/abierto/enviado/revisado/rechazado/revocado/expirado.

5. Preparacion IA
   - Contratos de asistencia IA.
   - Servicio de aplicacion para armar contexto RCA.
   - Cliente AI Gateway abstracto.
   - Implementacion stub local para probar sin IA real.

6. Endurecimiento P0 standalone
   - Autenticacion/autorizacion standalone configurable.
   - Tenant configurable desde contexto autenticado/configurado.
   - Roles base para operaciones sensibles.
   - Auditoria inicial persistida para cierre, escalamiento 8D, acciones,
     evidencias e intake externo interno.
   - Errores API normalizados con `ApiResult` para validaciones, 401/403 y
     excepciones no controladas bajo `/api`.
   - Hardening inicial de adjuntos con limite configurable y validacion de ruta
     dentro del storage root.
   - Descarga controlada de adjuntos validada por smoke comparando bytes,
     content-type y content-disposition.
   - Consulta protegida de auditoria por incidente mediante
     `GET /api/v1/rca/incidents/{id}/audit`.
   - Smoke local API + DB con flujo critico de cierre, evidencias, acciones,
     escalamiento 8D, cierre, eventos, IA stub, errores API, auditoria y facts
     externos.

## Estado de Corte P0

P0 queda cerrado como modulo standalone pilotable. El corte cumple build limpio,
suite base de tests livianos, smokes API + DB versionados, tenant configurable,
roles sobre operaciones sensibles, auditoria persistida/consultable, errores API
normalizados y adjuntos trazables con hash, limites y descarga controlada.

Los puntos que dependen de plataforma corporativa se mantienen como pendientes
post-P0: Identity global real, tenant multitenant corporativo, storage
documental productivo, CI/CD, tests MVC/UI, outbox/event bus, reapertura
gobernada y reportes corporativos de auditoria.

## Pendientes Tecnicos Inmediatos

- Definir integracion futura con Identity global y tenant corporativo real.
- Agregar suite formal ampliada de tests unitarios/integracion sobre la base de
  smokes y pruebas livianas actuales.
- Reemplazar `StubRcaAiGatewayClient` por cliente HTTP cuando exista el AI Gateway compartido.
- Implementar outbox/event bus real para eventos, si la plataforma global lo requiere.
- Extender adjuntos binarios/documentales al intake externo.
- Extender UI/API de auditoria fina, reapertura gobernada y reportes cuando se
  defina el consumidor corporativo.
- Endurecer generacion PDF con plantilla corporativa, firma/aprobacion y versionado documental cuando se defina gobierno documental.
- Evaluar storage documental productivo para evidencias y exportaciones generadas.
- Ampliar hardening de adjuntos con validacion de content-type/firma cuando se
  defina politica documental productiva.
- Mantener `run-local-validation.ps1 -Build` como validacion P0 por corte
  backend significativo.

## Siguiente Corte Recomendado

El corte P1 visual queda cerrado como experiencia industrial del detalle RCA.
Ya avanzaron command bar, KPI rail, CAPA board, tarjetas enriquecidas del
fishbone, zoom/pan del fishbone, timeline filtrable, panel lateral contextual,
estados UI, refuerzos responsive/tablet y drag/reorder visual de causas,
validados con build y tests livianos en serie.

El corte P2 de integracion operacional real queda iniciado con la documentacion
formal del feed de eventos RCA en `docs/INTEGRATION_EVENTS.md`. Ese documento
define envelope `RcaDomainEventDto`, compatibilidad `/api/v1`, deduplicacion
por `id`, polling por `occurredAt`, reglas para `data` extensible y la
evolucion esperada hacia outbox, webhooks, SignalR o broker sin acoplar
consumidores externos al modelo interno. La suite liviana ahora cubre ese
contrato contra el servicio in-memory para proteger envelope, correlacion
externa, tipos de evento documentados, claves `data` criticas y filtro
incremental `since`.

Tambien queda definida la especificacion tecnica inicial de outbox/webhooks en
`docs/superpowers/specs/2026-06-11-p2-rca-outbox-webhooks-design.md`. El
siguiente incremento recomendado es crear entidad, mapping y migracion
`RcaOutboxEvent`, sin reemplazar todavia el feed derivado. El plan ejecutable
esta en `docs/superpowers/plans/2026-06-11-p2-rca-outbox-base.md`.
La primera tarea del plan ya agrego el modelo de dominio `RcaOutboxEvent` y
`RcaOutboxEventStatus`. Tambien quedo agregada la migracion
`AddRcaOutboxEvents` para crear `rca_outbox_events`. El servicio base
`IRcaOutboxService` / `EfRcaOutboxService` ya existe para enqueue idempotente y
cambios de estado. La primera captura automatica ya se conecto para eventos de
alto valor: RCA creado, accion completada, fact registrado y RCA cerrado. El
feed `/api/v1/integrations/rca/events` sigue derivado hasta que el outbox iguale
su cobertura. La captura del servicio RCA principal ahora tambien cubre causas,
acciones creadas, evidencias, wizard y escalamiento 8D. El intake externo ya
fue conectado al outbox para created/opened/submitted/reviewed/rejected/revoked/
expired, por lo que la captura cubre los tipos actuales del feed derivado.
El endpoint `/api/v1/integrations/rca/events` ahora combina outbox y feed
derivado historico con deduplicacion por `id`. Siguen pendientes publicador,
webhooks y endpoints operativos de reintento/dead-letter. Ya existe
observabilidad basica del outbox mediante
`GET /api/v1/integrations/rca/outbox/status`, protegido por roles de gobierno
de calidad. Tambien queda preparada la configuracion `RcaIntegration` para el
futuro publicador/webhooks, con webhooks vacios y apagados por default. La
consulta read-only `GET /api/v1/integrations/rca/outbox/dead-letter?take=`
permite diagnosticar eventos enviados a dead-letter, y
`POST /api/v1/integrations/rca/outbox/{id}/retry` permite reprogramar eventos
fallidos o dead-letter a `Pending`.
El publicador outbox ya tiene base de servicio y resultado; en el estado actual
solo valida el camino seguro sin webhooks habilitados, evitando leer pendientes
o modificar el outbox. Tambien existe `IRcaWebhookSender` y el publicador ya
puede marcar eventos como `Published` si un sender abstracto entrega con exito
a todos los webhooks aplicables. El sender HTTP real `RcaHttpWebhookSender`
publica por POST el payload outbox, headers de evento y firma HMAC SHA-256
cuando el webhook tiene `Secret`.
Si algun destino aplicable falla, el publicador marca el evento como `Failed`,
guarda el error resumido y programa un reintento inicial a 1 minuto.
Al alcanzar `RcaIntegration:MaxPublishAttempts`, el evento pasa a `DeadLetter`.
El endpoint protegido `POST /api/v1/integrations/rca/outbox/publish` permite
disparar manualmente el publicador. El sender HTTP aplica
`RcaIntegration:PublishTimeoutSeconds` por request y transforma destinos lentos
en fallos controlados del outbox. El canal
`GET /api/v1/integrations/rca/events/live` expone Server-Sent Events para
timeline y estados usando el mismo envelope `RcaDomainEventDto`.

- Siguiente decision tecnica: persistencia de orden de causas, edicion avanzada
  desde panel lateral y regla formal de SLA visual requieren contrato/regla
  antes de implementarse.
- Siguiente paso P2 recomendado: agregar smoke manual de webhook y cerrar la
  matriz de alcance P2; la entrega HTTP basica, firma HMAC, timeout
  configurado, canal live SSE, fallo con backoff inicial,
  dead-letter por maximos intentos, observabilidad, dead-letter, retry manual,
  endpoint publish y base del publicador ya quedaron cubiertas.
- Validacion visual completa queda recomendada cuando se levante app + DB sin
  penalizar cada micro-ajuste.
- Mantener pendiente post-P0 el endurecimiento tecnico: suite formal de tests,
  permisos productivos refinados e integracion futura con Identity/tenant
  corporativo.
- Workflow RCA avanzado: CAPA, auditoria ampliada, aprobaciones, versionado PDF y adjuntos externos.
