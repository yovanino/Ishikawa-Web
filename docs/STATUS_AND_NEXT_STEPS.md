# Estado y Pendientes

Fecha de corte: 2026-06-01.

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
   - Carga de causas con puntajes.
   - Marcado de causa raiz.
   - Acciones correctivas.
   - Registro inicial de evidencias RCA con tipo, fuente, resumen, URI/referencia y relacion opcional a causa o intake externo.

4. Integracion externa
   - Endpoints versionados `/api/v1`.
   - Snapshots para Gantt, Gateway, OEE, Andon o app global.
   - Feed derivado de eventos RCA.
   - Conteo de evidencia en snapshots de integracion.
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

- Aplicar migracion en MySQL con credenciales reales.
- Ejecutar smoke test contra la app levantada.
- Agregar autenticacion/autorizacion cuando se defina Identity global.
- Agregar tenant real en lugar del tenant demo usado por la UI.
- Agregar tests automatizados cuando el flujo se estabilice.
- Reemplazar `StubRcaAiGatewayClient` por cliente HTTP cuando exista el AI Gateway compartido.
- Implementar outbox/event bus real para eventos, si la plataforma global lo requiere.
- Agregar almacenamiento binario/documental real para adjuntos de evidencia e intake externo.
- Separar auditoria fina de aprobacion/rechazo y adjuntos reales.

## Siguiente Corte Recomendado

El proximo paso natural es elegir uno de estos dos caminos:

- Endurecimiento tecnico: tests automatizados, validaciones, manejo de errores y tenant/auth.
- Workflow RCA: evidencias adjuntas, CAPA, auditoria ampliada y permisos/roles.
