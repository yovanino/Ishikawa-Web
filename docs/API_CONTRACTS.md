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

## Crear Incidente desde Sistema Externo

```json
{
  "sourceSystem": "GANTT",
  "externalTaskId": "TASK-2026-0001",
  "externalEventId": null,
  "title": "Retraso por parada de maquina",
  "description": "La tarea no pudo avanzar por parada de prensa.",
  "severity": 3,
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

## Eventos de Dominio

Eventos previstos para publicar a futuro por Event Bus, webhook o SignalR:

- `RcaIncidentCreated`
- `RcaWizardStepCompleted`
- `RcaCauseCreated`
- `RcaRootCauseSelected`
- `RcaCorrectiveActionCreated`
- `RcaEvidenceAttached`
- `RcaSeverityChanged`
- `RcaEscalatedTo8D`
- `RcaClosed`

## Regla de Compatibilidad

Los contratos publicos deben versionarse con `/api/v1`. Los cambios incompatibles deben ir a una nueva version.

