# Logica de Funcionamiento

Fecha de corte: 2026-06-06.

Este documento es la memoria funcional viva del modulo Ishikawa RCA. Debe
actualizarse cada vez que cambie una regla de negocio, flujo, validacion,
estado, contrato, pantalla o pendiente funcional relevante.

## Proposito

Ishikawa RCA es un modulo standalone para analisis de causa raiz operacional.
Permite crear, analizar, documentar, validar, integrar y cerrar incidentes RCA
de produccion, mantenimiento, calidad o seguridad.

El modulo puede nacer desde carga manual o desde otro sistema, pero no depende
directamente de Gantt, SCADA, OEE, TPM, Andon, Identity, AI Gateway ni maestros
globales. La integracion se hace mediante APIs, snapshots, eventos derivados y
contratos versionados.

## Regla de Actualizacion Continua

Cada avance futuro debe actualizar este documento cuando afecte:

- Logica de alta, analisis, validacion, cierre o escalamiento RCA.
- Causas, subcausas, ramas Ishikawa o reglas del canvas.
- Acciones correctivas, preventivas, recurrencia o FUGA/no deteccion.
- Evidencias, adjuntos, validacion documental o trazabilidad.
- Facts operacionales, correlacion externa o idempotencia.
- Intake externo cliente/proveedor.
- Wizard, bloqueos, porcentaje o requisitos de avance.
- APIs, snapshots, eventos o contratos de integracion.
- IA asistida, aprobacion humana o fallback.
- Seguridad, tenant, roles, permisos o auditoria.
- Pendientes funcionales que cambien de estado.

La bitacora tematica correspondiente es
`docs/chats/LOGICA_DE_FUNCIONAMIENTO.md`.

## Modelo Funcional Central

La entidad principal es el incidente RCA. Un incidente concentra:

- Problema reportado.
- Severidad.
- Estado.
- Origen del reclamo.
- Actor del reclamo: area interna, cliente o proveedor.
- Linea, maquina, orden de trabajo y contexto industrial.
- Referencias externas para integracion.
- Ramas Ishikawa.
- Causas y subcausas.
- Acciones.
- Evidencias.
- Facts operacionales.
- Solicitudes de intake externo.
- Estado de wizard.
- Cierre formal o escalamiento 8D.

## Estados Principales

Estados de incidente RCA:

- `Draft`
- `Open`
- `InAnalysis`
- `WaitingValidation`
- `Closed`
- `EscalatedTo8D`
- `Cancelled`

Etapas del wizard:

- `Problem`
- `Causes`
- `Evidence`
- `Actions`
- `Validation`
- `Closed`

Estados de accion:

- `Open`
- `InProgress`
- `WaitingValidation`
- `Completed`
- `Cancelled`

Estados de intake externo:

- `Draft`
- `Sent`
- `Opened`
- `Submitted`
- `Reviewed`
- `Expired`
- `Revoked`
- `Rejected`

## Flujo Principal RCA

1. Se crea un incidente RCA con titulo, descripcion, severidad, origen,
   contexto industrial y actor del reclamo.
2. Al crearse, el sistema inicializa el canvas Ishikawa con ramas base.
3. El usuario carga causas dentro de ramas.
4. Una causa puede tener subcausas mediante `ParentCauseId`.
5. Una causa puede marcarse como causa raiz.
6. El usuario registra evidencias y puede asociarlas a causas o intake externo.
7. El usuario registra facts operacionales manuales o externos.
8. El usuario define acciones correctivas, preventivas o preventivas de
   recurrencia.
9. El wizard calcula avance, bloqueos y siguiente paso recomendado.
10. El RCA puede escalarse formalmente a 8D.
11. El RCA solo puede cerrarse cuando cumple las reglas de cierre.
12. El cierre genera estado, usuario, fecha, resumen y queda expuesto para API,
   snapshots, eventos y PDF.

## Canvas Ishikawa

El canvas organiza el analisis en ramas y causas.

Hoy hace:

- Crea ramas por defecto al abrir un RCA nuevo.
- Permite cargar causas por rama.
- Permite cargar subcausas.
- Guarda puntajes de probabilidad, impacto y frecuencia.
- Ordena causas priorizando causa raiz y mayor puntaje combinado.
- Permite marcar una o mas causas como raiz segun los datos actuales.

Le falta:

- Drag and drop.
- Zoom y pan.
- Auto-layout.
- Panel lateral de edicion.
- Context menu industrial.
- Reglas visuales avanzadas de priorizacion.

## Acciones y Resolucion

Las acciones tienen dos clasificaciones funcionales:

Tipo:

- `Corrective`
- `Preventive`
- `RecurrencePreventive`

Ambito:

- `RootCause`
- `Escape`

La logica de resolucion exige:

- Siempre debe existir una accion preventiva de recurrencia para causa raiz.
- Si existe analisis de FUGA/no deteccion (`Escape`), debe existir set completo
  de acciones para ese ambito: correctiva, preventiva y preventiva de
  recurrencia.
- Las acciones canceladas no bloquean como abiertas.
- Las acciones completadas requieren nota de validacion.

Pendiente:

- Convertir este modelo en un CAPA board mas explicito.
- Agregar auditoria fina de cambios de estado.
- Agregar roles/permisos sobre completar, cancelar o validar acciones.

## Evidencias

Las evidencias documentan la trazabilidad del RCA.

Hoy soportan:

- Metadata de evidencia.
- Tipo, fuente, fuente detallada y tags.
- Resumen.
- URI o referencia externa.
- Vinculo opcional con causa.
- Vinculo opcional con intake externo.
- Adjuntos reales.
- SHA-256 del adjunto.
- Descarga controlada por endpoint.
- Previews compactos en UI.
- Edicion de metadata.
- Reemplazo de adjunto.
- Eliminacion logica.
- Estado de validacion.
- Usuario, fecha y notas de validacion.

Estados de validacion:

- `PendingReview`
- `Validated`
- `Rejected`
- `Expired`

Reglas actuales:

- Para validar formalmente el RCA debe existir al menos una evidencia validada.
- Si una evidencia se marca como `Validated`, debe quedar trazabilidad del
  validador y notas.
- Los adjuntos se guardan localmente y se referencian por storage key, no por
  path publico.

Le falta:

- Storage documental productivo.
- Versionado formal de evidencias.
- Firma o aprobacion documental.
- Politica corporativa de retencion.
- Auditoria fina de reemplazos y eliminaciones.

## Facts Operacionales

Los facts son hechos operacionales asociados al RCA.

Pueden representar:

- Observaciones.
- Alarmas.
- Mediciones.
- Eventos de SCADA/Gateway.
- Datos de turno, maquina, linea, OT, material o lote.

Pueden vincularse a:

- Causa.
- Evidencia.
- Accion.
- Intake externo.

Regla de integracion externa:

- `ExternalSourceSystem` y `ExternalEventId` deben enviarse juntos.
- La combinacion por RCA es idempotente.
- Si un sistema externo reintenta el mismo evento, el modulo devuelve el hecho
  existente en lugar de duplicarlo.

Le falta:

- Smoke automatizado estable para API + DB.
- Canal live para que facts nuevos actualicen timeline en tiempo real.
- Auditoria ampliada para consumidores externos.

## Wizard y Bloqueos

El wizard no solo muestra avance: calcula requisitos y bloqueos.

Bloqueos actuales:

- Para `Causes`: debe existir al menos una causa.
- Para `Evidence`: debe existir al menos una evidencia.
- Para `Actions`: debe existir causa raiz y al menos una accion.
- Para `Validation`: no deben quedar acciones abiertas.
- Para `Validation`: debe cumplirse la politica de resolucion.
- Para `Validation`: debe existir evidencia validada.
- Para `Closed`: el incidente debe estar cerrado formalmente.

El progreso expone:

- Etapa actual.
- Siguiente etapa recomendada.
- Porcentaje.
- Checklist.
- Requisitos.
- Bloqueos.
- Metricas por etapa.

## Cierre Formal

El cierre formal registra:

- Estado `Closed`.
- Fecha de cierre.
- Usuario de cierre.
- Resumen de cierre.

Condiciones funcionales:

- Debe existir causa raiz.
- No deben quedar acciones abiertas.
- Debe cumplirse la politica de resolucion.
- Debe existir evidencia validada.
- El resumen de cierre es obligatorio.

Pendientes:

- Firma/aprobacion documental.
- Versionado del PDF de cierre.
- Reapertura logica gobernada.
- Auditoria fina del cierre.

## Escalamiento 8D

El RCA puede escalarse formalmente a 8D.

Hoy hace:

- Marca `EscalatedTo8D`.
- Registra fecha.
- Registra usuario.
- Registra motivo.
- Cambia estado a `EscalatedTo8D`.
- Publica evento derivado para integracion.

Pendientes:

- Borrador 8D asistido por IA.
- Flujo formal de aprobacion.
- Auditoria y roles especificos.

## Intake Externo Cliente/Proveedor

El intake externo permite que cliente o proveedor aporte informacion sin acceder
al modulo completo.

Flujo actual:

1. Usuario interno crea link externo.
2. El sistema genera token seguro.
3. Solo se guarda hash del token.
4. El link tiene expiracion.
5. Si el externo lo abre, pasa de `Sent` a `Opened`.
6. El externo envia descripcion, referencia, material, lote, contencion, causa
   propuesta, accion propuesta y resumen de evidencia.
7. La respuesta pasa a `Submitted`.
8. Usuario interno revisa.
9. Si aprueba, puede importar causa y/o accion.
10. Si rechaza, debe informar motivo.
11. Si no fue respondido, puede revocar el link.
12. Si vence, pasa a `Expired`.

Reglas:

- No hay acceso a navegacion interna.
- La respuesta externa no entra al RCA oficial sin revision humana.
- Rechazar conserva la respuesta para auditoria.

Pendientes:

- Adjuntos binarios/documentales en intake externo.
- Verificacion adicional de identidad externa.
- Notificaciones.
- Vinculo futuro con maestros globales de clientes/proveedores.

## Integracion

La integracion se hace por contratos publicos, no por referencias directas.

Hoy expone:

- APIs versionadas `/api/v1`.
- Snapshot reducido del RCA.
- Feed derivado de eventos.
- Wizard progress por API.
- Conteos y estados para consumidores externos.
- Referencias externas denormalizadas.

Eventos derivados actuales o previstos:

- `RcaIncidentCreated`
- `RcaWizardStepCompleted`
- `RcaCauseCreated`
- `RcaRootCauseSelected`
- `RcaCorrectiveActionCreated`
- `RcaCorrectiveActionCompleted`
- `RcaExternalIntakeCreated`
- `RcaExternalIntakeOpened`
- `RcaExternalIntakeSubmitted`
- `RcaExternalIntakeReviewed`
- `RcaExternalIntakeRejected`
- `RcaExternalIntakeRevoked`
- `RcaExternalIntakeExpired`
- `RcaEvidenceAttached`
- `RcaFactRecorded`
- `RcaEscalatedTo8D`
- `RcaClosed`

Pendientes:

- Webhooks salientes.
- Outbox transaccional.
- Event bus.
- SignalR o canal live.
- Versionado formal de eventos.

## IA Asistida

La IA es opt-in y no es dependencia obligatoria.

Hoy hace:

- Arma contexto RCA con incidente, canvas y acciones.
- Expone sugerencia de causas.
- Expone sugerencia de acciones.
- Expone resumen.
- Usa cliente stub deterministico.
- Devuelve metadata de proveedor, modelo y fallback.

Reglas:

- La IA no ejecuta acciones industriales.
- La IA no cierra RCA.
- La IA no acepta respuestas externas.
- Toda decision oficial debe quedar bajo aprobacion humana.

Pendientes:

- Cliente HTTP real contra AI Gateway.
- Deteccion de recurrencia.
- Comparacion con RCA anteriores.
- Borrador 8D.
- UI de aceptar/rechazar sugerencias.
- Auditoria de sugerencias aceptadas.

## UI Funcional

La UI MVC actual permite:

- Crear RCA.
- Listar RCA.
- Ver detalle RCA.
- Ver canvas.
- Cargar causas y subcausas.
- Cargar acciones.
- Completar/cancelar acciones.
- Cargar facts.
- Cargar, editar, reemplazar, descargar y eliminar evidencias.
- Ver previews compactos.
- Avanzar wizard.
- Ver bloqueos.
- Crear y gestionar intake externo.
- Cerrar RCA.
- Escalar a 8D.
- Exportar PDF.

Pendientes UI:

- Cockpit/Obeya.
- Command bar del incidente.
- KPI rail.
- Fishbone interactivo.
- CAPA board.
- Timeline filtrable.
- Estados empty/loading/error/offline.
- Validacion responsive/tablet.

## Seguridad y Gobierno

Hoy existe:

- Autenticacion standalone configurable.
- Contexto de usuario/tenant para MVC/API.
- Roles base: `Operator`, `Supervisor`, `Quality`, `Maintenance` y
  `Administrator`.
- Autorizacion por roles para operaciones sensibles: cierre RCA, escalamiento
  8D, completar/cancelar acciones, validar/editar evidencia, reemplazar o
  eliminar adjuntos y gestion interna de intake externo.
- Auditoria inicial persistida en `rca_audit_records` para cierre RCA,
  escalamiento 8D, cambios de estado de acciones, cambios sensibles de
  evidencia y revision/rechazo/revocacion de intake externo.
- Token hasheado para intake externo.
- Expiracion y revocacion de links externos.
- Antiforgery en formularios MVC.
- Descarga controlada de adjuntos.

Falta:

- Integracion con Identity global.
- Tenant real resuelto desde identidad corporativa o proveedor multitenant.
- Refinar permisos por operacion y rol productivo.
- Consultas/reportes de auditoria fina.
- Politica productiva de secretos.
- Gobierno documental productivo.

## QA y Validacion

Hoy existe:

- Tests livianos de politica de resolucion.
- Tests livianos de facts externos e idempotencia.
- Scripts locales de arranque y smoke.

Falta:

- Suite formal de tests unitarios.
- Tests de integracion API.
- Tests MVC/UI.
- Smoke API + DB estable.
- CI/CD con validacion.

## Pendientes Priorizados

P0:

- Auth/autorizacion.
- Tenant real.
- Roles minimos.
- Tests base.
- Smoke API + DB.
- Auditoria fina inicial.
- Manejo consistente de errores.
- Hardening de adjuntos.

P1:

- Cockpit industrial.
- Fishbone interactivo.
- CAPA board.
- Timeline filtrable.
- Responsive/tablet.

P2:

- Webhooks.
- Outbox.
- Event bus.
- SignalR/live.
- Integraciones reales.

P3:

- AI Gateway HTTP.
- Recurrencia.
- Borrador 8D.
- Aprobacion humana de sugerencias.

P4:

- Storage documental productivo.
- PDF versionado.
- Firma/aprobacion.
- Integracion con Identity y maestros globales.

## Fuentes de Verdad Relacionadas

- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/API_CONTRACTS.md`
- `docs/AI_INTEGRATION.md`
- `docs/EXTERNAL_CLAIM_INTAKE.md`
- `docs/VALIDATION_LOG.md`
- `docs/chats/LOGICA_DE_FUNCIONAMIENTO.md`
