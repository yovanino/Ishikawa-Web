# Contratos API y Eventos

Los contratos deben permitir que el modulo funcione de forma independiente y sea consumido por otros modulos o por la plataforma global futura.

## Patron de Respuesta

```csharp
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();
    public string? CorrelationId { get; set; }
}

public class ApiError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

## APIs Iniciales

```http
POST /api/v1/rca/incidents
GET  /api/v1/rca/incidents/{id}
GET  /api/v1/rca/incidents?sourceSystem=&externalTaskId=&status=
POST /api/v1/rca/incidents/{id}/wizard/step
GET  /api/v1/rca/incidents/{id}/canvas
PUT  /api/v1/rca/incidents/{id}/canvas
POST /api/v1/rca/incidents/{id}/causes
POST /api/v1/rca/incidents/{id}/actions
POST /api/v1/rca/incidents/{id}/close
POST /api/v1/rca/incidents/{id}/escalate-8d
```

## Intake Externo Cliente/Proveedor

El MVP de intake externo se expone como flujo MVC controlado, no como API publica completa:

```http
POST /Rca/CreateExternalIntake/{incidentId}
POST /Rca/RevokeExternalIntake/{incidentId}?intakeId={intakeId}
POST /Rca/ReviewExternalIntake/{incidentId}
GET  /external-intake/{token}
POST /external-intake/{token}
```

El token se entrega una sola vez al usuario interno al generar el link. En base de datos se guarda solo `TokenHash`.

Respuesta externa esperada:

```json
{
  "contactName": "Contacto proveedor",
  "contactEmail": "proveedor@example.com",
  "claimReference": "SUP-2026-001",
  "materialCode": "MAT-01",
  "batchOrLot": "LOT-42",
  "description": "Descripcion del desvio observado por el proveedor.",
  "containmentResponse": "Lote bloqueado y segregado.",
  "proposedRootCause": "Desvio en control de proceso.",
  "proposedCorrectiveAction": "Ajustar control final y enviar certificado.",
  "evidenceSummary": "Certificado y fotos pendientes de adjuntar en fase posterior."
}
```

Estados del intake: `Sent`, `Opened`, `Submitted`, `Reviewed`, `Expired`, `Revoked`.

Revision interna esperada:

```json
{
  "intakeId": "8b30e5b6-dadb-4886-8d3e-4e39ce189bb4",
  "branchId": "2a70bcb5-2423-4b2a-95b0-9a7140a8ca6f",
  "importCause": true,
  "markCauseAsRoot": false,
  "importCorrectiveAction": true,
  "reviewedByUserId": "calidad"
}
```

Cuando se aprueba, el modulo puede crear una causa Ishikawa desde `proposedRootCause`, una accion correctiva desde `proposedCorrectiveAction` y marca el intake como `Reviewed`.

## APIs de Integracion entre Modulos

Estas APIs son la superficie recomendada para Gantt, gateway industrial, OEE, Andon, TPM o la futura app global. Devuelven una vista estable y reducida del RCA sin exponer el modelo interno completo.

```http
GET /api/v1/integrations/rca/snapshots?sourceSystem=&externalTaskId=&status=
GET /api/v1/integrations/rca/incidents/{id}/snapshot
GET /api/v1/integrations/rca/events?incidentId=&since=
```

## APIs de Asistencia IA

Estas APIs son opt-in. El RCA debe seguir funcionando aunque la IA este apagada o en modo stub.

```http
POST /api/v1/rca/incidents/{id}/ai/suggest-causes
POST /api/v1/rca/incidents/{id}/ai/suggest-actions
POST /api/v1/rca/incidents/{id}/ai/summarize
```

La respuesta incluye `metadata.provider`, `metadata.model` e `metadata.isFallback` para que la UI o la app global sepan si la recomendacion vino de IA real o de fallback.

Snapshot de integracion:

```json
{
  "incidentId": "2f5b0d57-53a4-4ac8-92fb-55d1b43753e0",
  "sourceSystem": "GANTT",
  "externalTaskId": "TASK-2026-0001",
  "status": "Open",
  "severity": "High",
  "claimScope": "Internal",
  "claimActorType": "InternalArea",
  "claimOwnerName": "Produccion",
  "rootCauseTitle": "Falta de lubricacion en prensa",
  "openCorrectiveActionsCount": 2,
  "overdueCorrectiveActionsCount": 0,
  "nextActionDueAt": "2026-05-30T12:00:00-03:00",
  "openActions": [
    {
      "id": "b06d64d4-a921-475d-99cf-9ef6df669ae2",
      "title": "Revisar plan de lubricacion",
      "status": "Open",
      "assignedToUserId": "mantenimiento",
      "dueDate": "2026-05-30T12:00:00-03:00"
    }
  ]
}
```

## Crear Incidente desde Sistema Externo

```json
{
  "sourceSystem": "GANTT",
  "externalTaskId": "TASK-2026-0001",
  "externalEventId": null,
  "title": "Retraso por parada de maquina",
  "problemDescription": "La tarea no pudo avanzar por parada de prensa.",
  "severity": "High",
  "claimScope": "Internal",
  "claimActorType": "InternalArea",
  "claimOwnerName": "Produccion",
  "occurredAt": "2026-05-28T09:15:00-03:00",
  "machineCode": "PRENSA-04",
  "lineCode": "LINEA-01",
  "workOrderCode": "WO-2026-000123",
  "reportedBy": "supervisor.planta",
  "contextSnapshot": {
    "taskName": "Produccion lote A",
    "plannedStart": "2026-05-28T08:00:00-03:00",
    "plannedEnd": "2026-05-28T12:00:00-03:00"
  }
}
```

`claimScope` agrupa el reclamo:

- `Internal`: reclamo interno, donde `claimOwnerName` representa el area solicitante.
- `External`: reclamo externo, donde `claimOwnerName` representa cliente o proveedor.

`claimActorType` identifica el actor especifico:

- `InternalArea`: area interna.
- `Customer`: cliente.
- `Supplier`: proveedor.

Para compatibilidad, si un consumidor viejo envia `claimScope = External` sin `claimActorType`, el modulo lo interpreta como `Customer`.

## Eventos de Dominio

Eventos previstos para publicar a futuro por Event Bus, webhook o SignalR:

- `RcaIncidentCreated`
- `RcaWizardStepCompleted`
- `RcaCauseCreated`
- `RcaRootCauseSelected`
- `RcaCorrectiveActionCreated`
- `RcaExternalIntakeCreated`
- `RcaExternalIntakeOpened`
- `RcaExternalIntakeSubmitted`
- `RcaExternalIntakeReviewed`
- `RcaExternalIntakeRevoked`
- `RcaExternalIntakeExpired`
- `RcaEvidenceAttached`
- `RcaSeverityChanged`
- `RcaEscalatedTo8D`
- `RcaClosed`

En esta etapa el endpoint `/api/v1/integrations/rca/events` expone un feed derivado de la informacion persistida. Incluye incidentes, causas, acciones e intake externo cliente/proveedor. En fases posteriores puede reemplazarse por outbox transaccional, webhook, SignalR o broker de eventos sin cambiar los consumidores externos.

## Regla de Compatibilidad

Los contratos publicos deben versionarse con `/api/v1`. Los cambios incompatibles deben ir a una nueva version.

