# Eventos de Integracion RCA

Fecha de corte: 2026-06-11.

Este documento define el contrato operativo y las reglas de compatibilidad del
feed de eventos RCA expuesto para consumidores externos. El objetivo P2 es que
Gantt, Gateway/SCADA, OEE, Andon, TPM, app global u otros consumidores puedan
seguir el RCA sin leer tablas internas ni depender de la UI MVC.

## Endpoint Actual

```http
GET /api/v1/integrations/rca/events?incidentId=&since=
```

El endpoint devuelve `ApiResult<IReadOnlyList<RcaDomainEventDto>>`.

- `incidentId` filtra eventos de un RCA puntual.
- `since` filtra por `occurredAt`.
- El feed actual es derivado desde el estado persistido del RCA y esta limitado
  a los eventos reconstruibles desde incidentes no eliminados.
- El endpoint mantiene orden ascendente por `occurredAt` para facilitar polling
  incremental.

## Envelope Estable

```json
{
  "id": "RcaIncidentCreated:2f5b0d57-53a4-4ac8-92fb-55d1b43753e0",
  "type": "RcaIncidentCreated",
  "occurredAt": "2026-06-11T10:30:00-03:00",
  "incidentId": "2f5b0d57-53a4-4ac8-92fb-55d1b43753e0",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "sourceSystem": "GANTT",
  "externalTaskId": "TASK-2026-0001",
  "externalEventId": null,
  "externalWorkOrderId": "WO-2026-000123",
  "data": {
    "title": "Retraso por parada de maquina",
    "status": "Open"
  }
}
```

Campos:

- `id`: identificador estable del evento dentro del modelo de feed derivado.
  Los consumidores deben usarlo para deduplicacion/idempotencia.
- `type`: nombre del evento de dominio RCA.
- `occurredAt`: fecha/hora de ocurrencia en formato ISO 8601.
- `incidentId`: RCA al que pertenece el evento.
- `tenantId`: tenant del RCA.
- `sourceSystem`, `externalTaskId`, `externalEventId`,
  `externalWorkOrderId`: correlacion con sistemas externos cuando exista en el
  incidente.
- `data`: diccionario extensible de valores string o null.

## Reglas de Compatibilidad

- `/api/v1` mantiene compatibilidad hacia atras para envelope, nombres de
  eventos y significado de claves documentadas.
- Agregar nuevas claves a `data` es compatible. Los consumidores deben ignorar
  claves desconocidas.
- Remover, renombrar o cambiar el significado de una clave documentada requiere
  una nueva version de API o un evento sucesor.
- Agregar nuevos tipos de evento es compatible si el consumidor ignora tipos no
  reconocidos.
- Los valores de `data` son strings o null por contrato. El consumidor que
  necesite fechas, booleanos o numeros debe parsearlos de forma explicita.
- `occurredAt` es el campo de avance para polling incremental. El consumidor
  debe guardar el ultimo `occurredAt` procesado y deduplicar por `id`.
- El feed actual no es todavia un outbox transaccional. Cuando se implemente
  outbox, webhook, SignalR o broker, debe conservarse el envelope o publicarse
  un sucesor versionado.
- La entrega esperada por polling es al-menos-leida. La entrega futura por
  webhook/broker debe asumirse al-menos-una-vez e idempotente por `id`.

## Tipos de Evento Actuales

Incidente:

- `RcaIncidentCreated`: RCA creado.
- `RcaClosed`: cierre formal.
- `RcaEscalatedTo8D`: escalamiento formal.

Wizard:

- `RcaWizardStepCompleted`: etapa RCA completada.

Causas:

- `RcaCauseCreated`: causa o subcausa creada.
- `RcaRootCauseSelected`: causa marcada como raiz.

Acciones:

- `RcaCorrectiveActionCreated`: accion creada.
- `RcaCorrectiveActionCompleted`: accion completada.

Evidencia y hechos:

- `RcaEvidenceAttached`: evidencia registrada.
- `RcaFactRecorded`: hecho operacional registrado.

Intake externo:

- `RcaExternalIntakeCreated`
- `RcaExternalIntakeOpened`
- `RcaExternalIntakeSubmitted`
- `RcaExternalIntakeReviewed`
- `RcaExternalIntakeRejected`
- `RcaExternalIntakeRevoked`
- `RcaExternalIntakeExpired`

## Claves de Datos por Familia

Las claves siguientes son la superficie minima documentada. Puede haber claves
aditivas mientras se respete la compatibilidad de `/api/v1`.

Incidente:

- Creacion: `title`, `severity`, `status`, `claimScope`,
  `claimActorType`, `claimOwnerName`.
- Cierre: `title`, `status`, `closedByUserId`, `closureSummary`.
- Escalamiento 8D: `title`, `status`, `escalatedByUserId`,
  `escalationReason`.

Wizard:

- `title`, `step`, `completedByUserId`, `notes`.

Causas:

- `causeId`, `branchId`, `parentCauseId`, `title`, `isRootCause`.

Acciones:

- Creacion: `actionId`, `causeId`, `title`, `status`, `dueDate`.
- Finalizacion: `actionId`, `causeId`, `title`, `status`,
  `completedByUserId`, `validationNotes`.

Evidencia:

- `evidenceId`, `causeId`, `externalIntakeId`, `title`, `evidenceType`,
  `source`, `sourceDetail`, `tags`, `validationStatus`,
  `validatedByUserId`, `referenceUri`, `attachmentFileName`.

Hechos operacionales:

- `factId`, `causeId`, `evidenceId`, `correctiveActionId`,
  `externalIntakeId`, `title`, `factType`, `source`, `sourceDetail`,
  `externalSourceSystem`, `externalEventId`, `externalRecordUri`.

Intake externo:

- `intakeId`, `actorType`, `actorName`, `contactEmail`, `status`,
  `expiresAt`.

## Guia para Consumidores

- Gantt debe correlacionar por `sourceSystem`, `externalTaskId` y
  `externalWorkOrderId`, y usar snapshots para estado actual.
- Gateway/SCADA debe ingresar hechos por API y luego seguir
  `RcaFactRecorded` para trazabilidad.
- OEE, Andon y TPM deben leer eventos como timeline operativo, no como modelo
  canonico interno.
- La app global debe combinar snapshots para vista actual y eventos para
  historial incremental.
- Ningun consumidor debe consultar tablas internas ni depender de nombres de
  clases de dominio.

## Evolucion P2

El proximo paso tecnico P2 es reemplazar o complementar el feed derivado con un
outbox transaccional y canales salientes configurables. Ese cambio debe:

- Mantener `RcaDomainEventDto` como contrato base o publicar un sucesor
  versionado.
- Preservar idempotencia por `id`.
- Registrar reintentos, errores de entrega y auditoria de consumo.
- Permitir que el modulo RCA siga funcionando standalone aunque los
  consumidores externos fallen.
