# Roadmap Maestro del Modulo Ishikawa RCA

Fecha de corte: 2026-06-06.

## Lectura Ejecutiva

El modulo Ishikawa RCA ya supero el estado de MVP basico. El repositorio tiene
una solucion ASP.NET Core por capas, persistencia MySQL, UI MVC operativa,
contratos API versionados, evidencias con adjuntos/hash/validacion, facts
operacionales, intake externo cliente/proveedor, wizard guiado, exportacion PDF,
feed de integracion y asistencia IA en modo stub.

El corte P0 standalone ya no debe leerse como "hacer que funcione"; queda
cerrado como base pilotable. Los siguientes cortes deben llevarlo hacia producto
industrial: UX de cockpit, integraciones salientes reales, Identity corporativo,
storage documental productivo y AI Gateway HTTP.

## Estado Actual Comprobado

### Arquitectura

- [x] Solucion modular ASP.NET Core.
- [x] Capas separadas: `Domain`, `Application`, `Contracts`,
  `Infrastructure`, `Web`.
- [x] Entidades de dominio para incidentes, ramas, causas, acciones,
  evidencias, facts e intake externo.
- [x] Servicios de aplicacion para RCA e intake externo.
- [x] Implementacion EF Core + MySQL.
- [x] Implementacion in-memory para pruebas/logica aislada.
- [x] Contratos API compartidos en proyecto `Contracts`.
- [x] Documentacion de contexto, limites, APIs, IA, operaciones locales,
  benchmark UI y modelo de chats Codex.

### Producto RCA

- [x] Alta, listado y detalle de incidentes RCA.
- [x] Clasificacion por severidad, estado, origen, linea, maquina, OT y actor
  del reclamo.
- [x] Canvas Ishikawa inicial con ramas 6M.
- [x] Causas y subcausas.
- [x] Seleccion de causa raiz.
- [x] Acciones correctivas.
- [x] Acciones clasificadas por tipo: correctiva, preventiva y preventiva de
  recurrencia.
- [x] Acciones clasificadas por ambito: causa raiz y FUGA/no deteccion.
- [x] Politica de cierre/resolucion que exige cobertura preventiva de
  recurrencia.
- [x] Escalamiento formal a 8D.
- [x] Cierre formal con resumen, usuario y fecha.

### Wizard y Flujo Guiado

- [x] Wizard con etapas `Problem`, `Causes`, `Evidence`, `Actions`,
  `Validation`, `Closed`.
- [x] Endpoint de avance de etapa.
- [x] Endpoint de progreso con porcentaje, bloqueos, metricas y siguiente paso.
- [x] UI con checklist, bloqueos y avance visual.

### Evidencias

- [x] Evidencias por metadatos.
- [x] Evidencias vinculables a causa e intake externo.
- [x] Adjuntos reales con storage local configurable.
- [x] Descarga controlada por endpoint.
- [x] SHA-256 de adjuntos.
- [x] Previews compactos por tipo de archivo.
- [x] Edicion, reemplazo de adjunto y eliminacion.
- [x] Estados de validacion, validador, fecha y notas.
- [ ] Storage documental productivo.
- [ ] Versionado formal de documentos/evidencias.
- [ ] Firma/aprobacion documental.

### Hechos Operacionales

- [x] Linea de hechos manual.
- [x] Facts vinculables a causa, evidencia, accion e intake externo.
- [x] Clasificacion industrial: severidad, turno, maquina, linea, OT,
  material, lote, alarma y medicion.
- [x] Ingreso de facts por API.
- [x] Correlacion externa con `externalSourceSystem`, `externalEventId` y
  `externalRecordUri`.
- [x] Idempotencia por RCA + sistema/evento externo.

### Intake Externo Cliente/Proveedor

- [x] Actor de reclamo: area interna, cliente o proveedor.
- [x] Link externo con token hasheado.
- [x] Expiracion, revocacion y estados de intake.
- [x] Pantalla externa sin navegacion completa.
- [x] Envio de respuesta externa.
- [x] Revision interna.
- [x] Importacion opcional de causa y accion correctiva.
- [x] Rechazo formal con motivo.
- [x] Eventos derivados de created/opened/submitted/reviewed/rejected/revoked/
  expired.
- [ ] Adjuntos binarios/documentales en intake externo.
- [ ] Verificacion adicional para actores externos cuando lo defina Identity.

### APIs e Integracion

- [x] Endpoints versionados `/api/v1`.
- [x] API de incidentes.
- [x] API de canvas/causas.
- [x] API de acciones.
- [x] API de evidencias y adjuntos.
- [x] API de facts.
- [x] API de cierre y escalamiento 8D.
- [x] API de wizard/progreso.
- [x] Snapshots de integracion.
- [x] Feed derivado de eventos RCA.
- [x] Contratos preparados para Gantt, SCADA/Gateway, OEE, Andon, TPM y app
  global.
- [ ] Webhooks salientes.
- [ ] Outbox/event bus transaccional.
- [ ] SignalR o canal live para actualizaciones.

### IA

- [x] Contratos de contexto y resultados IA.
- [x] Servicio de aplicacion para armar contexto RCA.
- [x] Cliente AI Gateway abstracto.
- [x] Stub deterministico local.
- [x] Endpoints de sugerencia de causas, acciones y resumen.
- [ ] Cliente HTTP real contra AI Gateway.
- [ ] Deteccion de recurrencia.
- [ ] Borrador 8D.
- [ ] Flujo UI de aprobacion humana de sugerencias.

### UI

- [x] UI MVC para crear/listar/ver RCA.
- [x] Detalle RCA con canvas, wizard, timeline, evidencias, facts, acciones,
  intake externo y cierre.
- [x] Exportacion PDF desde UI.
- [x] Gestion visual de evidencias.
- [x] Timeline unificado.
- [x] Drag and drop visual de causas.
- [x] Zoom y pan del canvas.
- [x] Panel lateral contextual.
- [ ] Context menu industrial.
- [ ] Auto-layout del fishbone.
- [ ] Comentarios colaborativos.
- [ ] Vistas por rol.
- [ ] Modo cockpit/Obeya para supervision.

### QA y Operacion

- [x] Scripts locales de arranque y smoke test.
- [x] Guia de operacion local.
- [x] Tests livianos de politica de resolucion, facts externos, storage de
  evidencias y auditoria in-memory.
- [x] Suite base de tests de dominio/aplicacion para politicas P0.
- [x] Tests de integracion API por smokes versionados.
- [ ] Tests MVC/UI.
- [x] Smoke automatizado estable contra DB local.
- [ ] Validacion CI/CD.

### Seguridad y Gobierno

- [x] Tokens hasheados para intake externo.
- [x] Antiforgery en formularios MVC.
- [x] Descarga controlada de adjuntos.
- [x] Autenticacion/autorizacion standalone configurable.
- [x] Tenant configurable en MVC en lugar de tenant demo hardcodeado.
- [x] Roles base para operaciones sensibles.
- [x] Auditoria inicial persistida para operaciones sensibles.
- [x] Consulta protegida de auditoria por incidente.
- [ ] Integracion con Identity global.
- [ ] Tenant real resuelto por identidad corporativa/multitenant.
- [x] Permisos P0 refinados por operacion sensible.
- [x] Consulta inicial de auditoria fina.
- [ ] Politica productiva de secretos/configuracion.

## Prioridad Recomendada

### P0 - Endurecimiento Producto Standalone

Objetivo: dejar Ishikawa RCA como modulo standalone confiable para uso piloto
industrial.

- [x] Autenticacion/autorizacion basica standalone.
- [x] Tenant configurable y eliminacion del tenant demo hardcodeado en MVC.
- [x] Roles minimos: operador, supervisor, calidad, mantenimiento,
  administrador.
- [x] Preparacion para integracion futura con Identity global y tenant
  corporativo real mediante contexto de usuario/tenant desacoplado.
- [x] Suite base de tests para politicas de dominio y validaciones P0.
- [x] Tests de integracion para endpoints criticos mediante smokes API.
- [x] Smoke test automatizado API + DB.
- [x] Auditoria inicial para cierre, revision/rechazo de intake
  y cambios de evidencia.
- [x] Auditoria fina inicial con consulta protegida por incidente.
- [x] Manejo consistente de errores API y validaciones MVC existentes.
- [x] Hardening de storage local y limites de adjuntos.
- [x] Actualizar `VALIDATION_LOG.md` por cada corte.

Estado del corte: P0 cerrado como producto standalone pilotable. Quedan fuera
del alcance P0 y pasan a cortes posteriores: Identity global real, tenant
corporativo multitenant, storage documental productivo, CI/CD, tests MVC/UI,
outbox/event bus, reapertura gobernada y reportes corporativos de auditoria.

Criterio de salida P0:

- Build limpio.
- Tests unitarios e integracion pasando.
- Smoke local documentado.
- Ningun flujo critico depende de tenant demo.
- Operaciones sensibles requieren usuario/rol.
- Adjuntos y evidencias mantienen trazabilidad verificable.

### P1 - Experiencia Visual Industrial

Objetivo: transformar la UI de detalle RCA en una superficie de trabajo tipo
cockpit industrial.

- [x] Command bar del incidente: estado, severidad, linea, maquina,
  responsable, fase actual.
- [ ] SLA visual del incidente cuando exista regla formal de SLA.
- [x] KPI rail compacto: causas abiertas, evidencias, acciones vencidas, edad
  de contencion, riesgo de recurrencia.
- [x] Fishbone con tarjetas de causa enriquecidas.
- [x] Interaccion avanzada del fishbone sobre tarjetas de causa.
- [x] Drag/reorder visual de causas dentro de rama.
- [ ] Persistencia de orden de causas si se define contrato.
- [x] Zoom, pan y fit-to-screen.
- [x] Panel lateral de detalle para causa/evidencia/accion.
- [ ] Edicion avanzada desde panel lateral.
- [x] Timeline operacional filtrable.
- [x] CAPA board separado por correctiva, preventiva y recurrencia.
- [x] Estados empty/loading/error/offline.
- [x] Validacion responsive/tablet.

Avance P1 inicial 2026-06-11: el detalle RCA ya tiene command bar industrial y
KPI rail ampliado. La validacion visual completa queda agrupada para el cierre
del bloque cockpit/tablet, no para cada micro-ajuste UI.

Avance P1 2026-06-11: la seccion de resolucion incorpora CAPA board separado
por accion correctiva, preventiva y preventiva de recurrencia, reutilizando las
acciones existentes y sin cambios de contrato.

Avance P1 2026-06-11: el fishbone muestra tarjetas de causa con puntajes P/I/F,
score total, subcausa, marca de causa raiz y trazabilidad a evidencias, hechos y
acciones.

Avance P1 2026-06-11: el timeline unificado incorpora filtros client-side por
hechos, evidencias, acciones, wizard y eventos externos.

Avance P1 2026-06-11: el fishbone incorpora toolbar client-side para acercar,
alejar, ajustar y paneo por arrastre dentro del tablero.

Avance P1 2026-06-11: se agrega panel lateral contextual para consultar detalle
de causas, evidencias y acciones desde sus tarjetas.

Avance P1 2026-06-11: se refuerzan estados empty con cards compactas, loading
visual en submits, banner offline y se mantienen errores de validacion MVC.

Avance P1 2026-06-11: se refuerzan reglas responsive/tablet para command bar,
fishbone, timeline filters, panel lateral, CAPA y botones contextuales.

Avance P1 2026-06-11: las tarjetas de causa del fishbone permiten drag/reorder
visual dentro de la misma rama sin persistencia nueva ni contrato backend.

Estado del corte: P1 visual queda cerrado como cockpit industrial standalone.
Quedan fuera del corte, por requerir contrato/regla adicional, SLA formal,
persistencia de orden de causas y edicion avanzada desde panel lateral.

Criterio de salida P1:

- El usuario puede navegar el RCA principal sin perder contexto.
- Las decisiones clave se ven en una sola pantalla.
- El canvas deja de ser solo dibujo y se comporta como artefacto vivo.
- La UI mantiene densidad industrial sin volverse decorativa.

### P2 - Integracion Operacional Real

Objetivo: conectar el RCA con otros modulos/sistemas sin acoplamiento directo.

- [ ] Webhooks configurables.
- [ ] Outbox transaccional.
- [ ] Contratos de eventos listos para broker/event bus.
- [ ] SignalR o canal live para timeline y estados.
- [ ] Integracion API concreta con Gantt.
- [ ] Integracion API concreta con Gateway/SCADA para facts.
- [ ] Estados y auditoria ampliada para consumidores externos.
- [x] Documentar versionado y compatibilidad de eventos.

Avance P2 2026-06-11: queda documentado el contrato de eventos de integracion,
incluyendo envelope `RcaDomainEventDto`, reglas de compatibilidad `/api/v1`,
deduplicacion por `id`, polling por `occurredAt` y evolucion futura hacia
outbox/webhooks/SignalR sin acoplar consumidores al modelo interno.

Avance P2 2026-06-11: se agrega cobertura liviana de compatibilidad para el
feed de eventos de integracion sobre el servicio in-memory. La prueba fija
envelope, correlacion externa, tipos documentados, claves `data` criticas y
filtro incremental `since`.

Avance P2 2026-06-11: queda especificado el diseno tecnico recomendado para
outbox transaccional y webhooks en
`docs/superpowers/specs/2026-06-11-p2-rca-outbox-webhooks-design.md`. El orden
recomendado es outbox primero, webhooks despues, manteniendo el feed derivado
como compatibilidad hasta igualar cobertura.

Avance P2 2026-06-11: inicia la implementacion outbox con el modelo de dominio
`RcaOutboxEvent` y estados `Pending`, `Publishing`, `Published`, `Failed` y
`DeadLetter`. Todavia no hay tabla, publicador ni reemplazo del feed derivado.

Avance P2 2026-06-11: se agrega mapping EF y migracion
`AddRcaOutboxEvents` para `rca_outbox_events`, con idempotencia por
`TenantId + EventId` e indices para publicacion pendiente y consulta por RCA o
tipo de evento.

Criterio de salida P2:

- Los consumidores externos pueden crear, consultar y seguir un RCA sin acceder
  al modelo interno.
- Los eventos relevantes salen de forma confiable e idempotente.
- El modulo sigue funcionando standalone si los consumidores externos fallan.

### P3 - IA Asistida con Aprobacion Humana

Objetivo: pasar de stub IA a asistencia real gobernada.

- [ ] Cliente HTTP real para AI Gateway.
- [ ] Configuracion por ambiente/tenant.
- [ ] Sugerencias de causas con metadata de modelo/proveedor.
- [ ] Sugerencias de acciones CAPA.
- [ ] Resumen del RCA.
- [ ] Deteccion de recurrencia por historico.
- [ ] Borrador 8D.
- [ ] UI para aceptar/rechazar sugerencias.
- [ ] Auditoria de sugerencias aceptadas.

Criterio de salida P3:

- La IA no escribe decisiones oficiales sin aprobacion humana.
- Toda sugerencia aceptada queda trazable.
- El modulo opera correctamente con IA apagada o degradada.

### P4 - Gobierno Documental y Plataforma Global

Objetivo: preparar el modulo para produccion corporativa y convivencia con la
plataforma industrial mayor.

- [ ] Storage documental productivo.
- [ ] Versionado de PDF de cierre.
- [ ] Plantilla corporativa de PDF.
- [ ] Firma/aprobacion de cierre.
- [ ] Integracion con Identity global.
- [ ] Integracion con maestros globales de clientes/proveedores.
- [ ] Registro en app global.
- [ ] Dashboard cross-module.
- [ ] Timeline operacional unificado.

Criterio de salida P4:

- Los cierres RCA son auditables como documentos formales.
- El modulo puede integrarse a la plataforma sin reescritura interna.
- Clientes/proveedores se resuelven por maestros globales cuando existan.

## Secuencia Recomendada de Trabajo

1. Crear frente QA/P0: tests de dominio, API y smoke DB.
2. Crear frente Seguridad/P0: tenant real, auth, roles y permisos.
3. Crear frente Auditoria/P0: historial fino de acciones sensibles.
4. Crear frente UI/P1: cockpit del detalle RCA.
5. Crear frente Integracion/P2: outbox/webhooks y live updates.
6. Crear frente IA/P3: cliente HTTP AI Gateway y aprobacion humana.
7. Crear frente Documental/P4: storage, PDF versionado y aprobaciones.

## Riesgos Principales

- Avanzar UI premium sin cerrar seguridad y tenant real puede producir una demo
  vistosa pero no pilotable.
- Agregar integraciones directas con otros modulos puede romper la decision de
  arquitectura standalone.
- Usar IA sin aprobacion humana puede degradar trazabilidad y confianza.
- Mantener solo tests de consola deja sin cobertura flujos API/MVC criticos.
- El storage local de adjuntos sirve para MVP, pero debe endurecerse antes de
  produccion.

## Proximo Corte Recomendado

El siguiente corte debe salir de P0 y elegir foco:

- P1 si se prioriza experiencia visual industrial/cockpit.
- P2 si se prioriza integracion operacional real.
- P3 si se prioriza AI Gateway con aprobacion humana.
- P4 si se prioriza gobierno documental y plataforma global.

El corte P0 ya dejo entregado:

- Auth/roles/tenant real minimos.
- Suite de tests base.
- Smoke API + DB confiable.
- Auditoria fina inicial.
- Validacion documentada en `docs/VALIDATION_LOG.md`.

Con ese piso, el proyecto queda en condiciones de invertir fuerte en UI cockpit,
integraciones reales, IA gobernada o gobierno documental sin reabrir la base
standalone.
