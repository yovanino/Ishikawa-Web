# Limites del Modulo

## Este Modulo Incluye

- Wizard Ishikawa.
- Canvas visual de espina de pescado.
- Gestion de analisis RCA.
- Ramas/categorias configurables.
- Causas y subcausas.
- Evidencias.
- Acciones correctivas.
- Estados y severidad.
- Historial basico.
- APIs de integracion.
- Eventos de dominio.
- Preparacion para AI Gateway.

## Este Modulo No Incluye

- Gantt completo.
- SCADA completo.
- Industrial Communication Gateway.
- OEE Live.
- TPM completo.
- Andon completo.
- Plataforma global multi-modulo.
- Motor no-code de reglas.
- Digital Twin.
- Modelo local de IA embebido dentro del modulo.

## Integracion con Gantt

El Gantt se integra por APIs de entrada/salida.

Ishikawa RCA puede recibir:

- Id externo de tarea.
- Nombre de tarea.
- Fechas.
- Responsable.
- Orden de trabajo.
- Maquina o linea asociada.
- Problema reportado.
- Severidad inicial.

Ishikawa RCA puede devolver:

- Estado RCA.
- Severidad actual.
- Causa raiz.
- Acciones abiertas.
- Fecha de cierre.
- Escalamiento a 8D.

La salida recomendada para Gantt es el snapshot de integracion:

```http
GET /api/v1/integrations/rca/snapshots?sourceSystem=GANTT&externalTaskId={taskId}
```

## Integracion con SCADA y Gateway Industrial

SCADA o el gateway pueden crear incidentes RCA a partir de eventos industriales:

- Alarma PLC.
- Maquina detenida.
- Temperatura fuera de rango.
- Setup excedido.
- Velocidad fuera de estandar.
- Recurrencia de falla.

La primera integracion sera por REST API. SignalR, MQTT y Event Bus quedan para fases posteriores.

La salida recomendada para el gateway o una app global es:

```http
GET /api/v1/integrations/rca/events?since={isoDate}
GET /api/v1/integrations/rca/incidents/{id}/snapshot
```

El modulo no consume directamente PLC, SCADA, Gantt ni IA local. Esos sistemas se conectan por contratos HTTP versionados, y mas adelante por eventos o colas.

