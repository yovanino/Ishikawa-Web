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
POST /api/v1/rca/incidents/{id}/actions/{actionId}/status
GET  /api/v1/rca/incidents/{id}/evidence
POST /api/v1/rca/incidents/{id}/evidence
POST /api/v1/rca/incidents/{id}/evidence-files
GET  /api/v1/rca/incidents/{id}/evidence/{evidenceId}/attachment
POST /api/v1/rca/incidents/{id}/close
POST /api/v1/rca/incidents/{id}/escalate-8d
```

Wizard RCA:

```json
{
  "step": "Causes",
  "completedByUserId": "calidad",
  "notes": "Causas iniciales cargadas y listas para evidencia."
}
```

Etapas validas: `Problem`, `Causes`, `Evidence`, `Actions`, `Validation`, `Closed`. El endpoint valida prerequisitos minimos para avanzar: causas cargadas, evidencia cargada, acciones cargadas, acciones sin pendientes y RCA cerrado para la etapa `Closed`. El snapshot de integracion expone `wizardStep` para que Gantt, AI Gateway, app global u otros modulos consuman el avance sin acoplarse a la UI.

Escalamiento a 8D:

```json
{
  "escalatedByUserId": "calidad",
  "escalationReason": "Impacto critico o recurrencia que requiere disciplina 8D formal."
}
```

El escalamiento a 8D marca `escalatedTo8D = true`, registra fecha/usuario/motivo, cambia el estado a `EscalatedTo8D` y publica `RcaEscalatedTo8D`. El RCA puede luego cerrarse formalmente cuando cumpla las condiciones de cierre.

Evidencia con adjunto:

```http
POST /api/v1/rca/incidents/{id}/evidence-files
Content-Type: multipart/form-data
```

Campos de formulario: `Attachment` obligatorio, `title`, `evidenceType`, `source`, `summary`, `causeId`, `externalIntakeId`, `capturedByUserId`, `capturedAt`, `referenceUri`. El almacenamiento local MVP acepta PDF, Office, CSV/TXT, imagenes y videos hasta 100 MB. El DTO devuelve nombre, content-type, tamano, storage provider, storage key y SHA-256. La descarga se hace por endpoint controlado, no por path publico.

Cierre formal RCA:

```json
{
  "closedByUserId": "calidad",
  "closureSummary": "Causa raiz confirmada, evidencia registrada y acciones correctivas verificadas."
}
```

Para cerrar un RCA debe existir causa raiz y no deben quedar acciones abiertas. El cierre actualiza `status = Closed`, `closedAt`, `closedByUserId`, `closureSummary` y publica `RcaClosed` en el feed de integracion.

Validacion/cierre de accion correctiva:

```json
{
  "status": "Completed",
  "completedByUserId": "calidad",
  "validationNotes": "Accion verificada en piso y evidencia asociada al RCA."
}
```

Para completar una accion, `validationNotes` es obligatorio. Esto mantiene el modulo standalone, pero deja trazabilidad suficiente para CAPA, auditoria y consumo desde Gantt/OEE/TPM.

Evidencia RCA inicial:

```json
{
  "causeId": "2a70bcb5-2423-4b2a-95b0-9a7140a8ca6f",
  "externalIntakeId": null,
  "title": "Foto de defecto en pieza",
  "evidenceType": "Photo",
  "source": "Manual",
  "summary": "Defecto visible luego de inspeccion final.",
  "referenceUri": "https://documentos.example/rca/evidencia-001",
  "capturedAt": "2026-06-01T10:30:00-03:00",
  "capturedByUserId": "calidad"
}
```

En esta fase se registran metadatos, resumen y URI/referencia de evidencia. El almacenamiento binario de archivos queda separado para la politica documental global.

Causas y subcausas:

```json
{
  "branchId": "2a70bcb5-2423-4b2a-95b0-9a7140a8ca6f",
  "parentCauseId": "9d9ff5be-7977-4276-a06f-aad82d15505f",
  "title": "Falta de inspeccion previa",
  "description": "Segundo nivel de analisis dentro de la rama Metodo.",
  "probabilityScore": 3,
  "impactScore": 4,
  "frequencyScore": 2,
  "isRootCause": false,
  "evidenceSummary": "Registro de turno y foto adjunta como evidencia RCA."
}
```

Si `parentCauseId` viene informado, la causa queda registrada como subcausa de una causa existente del mismo incidente.

## Intake Externo Cliente/Proveedor

El MVP de intake externo se expone como flujo MVC controlado, no como API publica completa:

```http
POST /Rca/CreateExternalIntake/{incidentId}
POST /Rca/RevokeExternalIntake/{incidentId}?intakeId={intakeId}
POST /Rca/ReviewExternalIntake/{incidentId}
POST /Rca/RejectExternalIntake/{incidentId}
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

Estados del intake: `Sent`, `Opened`, `Submitted`, `Reviewed`, `Rejected`, `Expired`, `Revoked`.

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

Rechazo interno esperado:

```json
{
  "intakeId": "8b30e5b6-dadb-4886-8d3e-4e39ce189bb4",
  "rejectionReason": "Informacion insuficiente para incorporarla al RCA.",
  "rejectedByUserId": "calidad"
}
```

Cuando se rechaza, el modulo no importa causa ni accion, conserva la respuesta externa para auditoria y marca el intake como `Rejected`.

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
  "evidenceCount": 1,
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
- `RcaCorrectiveActionCompleted`
- `RcaExternalIntakeCreated`
- `RcaExternalIntakeOpened`
- `RcaExternalIntakeSubmitted`
- `RcaExternalIntakeReviewed`
- `RcaExternalIntakeRejected`
- `RcaExternalIntakeRevoked`
- `RcaExternalIntakeExpired`
- `RcaEvidenceAttached`
- `RcaSeverityChanged`
- `RcaEscalatedTo8D`
- `RcaClosed`

En esta etapa el endpoint `/api/v1/integrations/rca/events` expone un feed derivado de la informacion persistida. Incluye incidentes, wizard, causas, acciones, evidencia e intake externo cliente/proveedor. En fases posteriores puede reemplazarse por outbox transaccional, webhook, SignalR o broker de eventos sin cambiar los consumidores externos.

## Regla de Compatibilidad

Los contratos publicos deben versionarse con `/api/v1`. Los cambios incompatibles deben ir a una nueva version.

