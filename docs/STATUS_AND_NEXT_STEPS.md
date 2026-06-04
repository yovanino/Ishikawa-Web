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
   - Linea de hechos manual RCA con fecha/hora, tipo, fuente, causa/evidencia/accion/intake opcional, API y exportacion PDF.

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

## Pendientes Tecnicos Inmediatos

- Agregar autenticacion/autorizacion cuando se defina Identity global.
- Agregar tenant real en lugar del tenant demo usado por la UI.
- Agregar tests automatizados cuando el flujo se estabilice.
- Reemplazar `StubRcaAiGatewayClient` por cliente HTTP cuando exista el AI Gateway compartido.
- Implementar outbox/event bus real para eventos, si la plataforma global lo requiere.
- Extender adjuntos binarios/documentales al intake externo.
- Separar auditoria fina de aprobacion/rechazo, historial completo de cambios de estado y adjuntos reales.
- Endurecer generacion PDF con plantilla corporativa, firma/aprobacion y versionado documental cuando se defina gobierno documental.
- Evaluar storage documental productivo para evidencias y exportaciones generadas.
- Extender la linea de hechos con clasificacion industrial avanzada, timeline unificado y entrada por API desde modulos externos.

## Siguiente Corte Recomendado

El proximo paso natural es elegir uno de estos dos caminos:

- Endurecimiento tecnico: tests automatizados, manejo de errores, tenant/auth, permisos y roles.
- Workflow RCA avanzado: CAPA, auditoria ampliada, aprobaciones, versionado PDF y adjuntos externos.
