# P2 RCA Outbox y Webhooks - Diseno

Fecha: 2026-06-11.

## Contexto

El modulo RCA ya expone `GET /api/v1/integrations/rca/events` como feed
derivado. Ese contrato esta documentado en `docs/INTEGRATION_EVENTS.md` y tiene
cobertura liviana en `tests/IshikawaRca.Tests`. El siguiente paso P2 debe
mejorar confiabilidad de entrega sin acoplar RCA a Gantt, SCADA, OEE, Andon,
TPM ni app global.

## Objetivo

Implementar una base de outbox transaccional y preparar webhooks salientes
configurables, manteniendo:

- Compatibilidad con `RcaDomainEventDto`.
- Operacion standalone aunque los consumidores fallen.
- Idempotencia por `eventId`.
- Reintentos/auditoria de entrega.
- Sin referencias directas a otros modulos.

## Enfoques Evaluados

### Opcion A - Webhooks directos desde cada operacion

Publicar HTTP al momento de crear causa, evidencia, accion o cierre.

Ventaja: simple de entender.

Problema: acopla el flujo RCA al consumidor externo. Si el endpoint remoto
falla o demora, afecta la operacion principal.

Decision: descartado.

### Opcion B - Outbox transaccional primero

Persistir eventos en una tabla `rca_outbox_events` dentro de la misma unidad de
trabajo que cambia el RCA. Un publicador posterior entrega por webhook/broker.

Ventaja: separa escritura RCA de entrega externa, permite reintentos e
idempotencia.

Problema: requiere migracion, mapping EF y una politica de publicacion.

Decision: recomendado.

### Opcion C - Solo mantener polling derivado

Conservar el endpoint actual y pedir a consumidores que hagan polling.

Ventaja: ya funciona.

Problema: no cubre entrega confiable, auditoria de despacho ni webhooks
configurables.

Decision: mantener como compatibilidad, no como destino final P2.

## Diseno Propuesto

### Entidad `RcaOutboxEvent`

Tabla propuesta: `rca_outbox_events`.

Campos minimos:

- `Id`: GUID interno.
- `TenantId`: tenant RCA.
- `EventId`: idempotency key estable, equivalente a `RcaDomainEventDto.Id`.
- `EventType`: nombre del evento.
- `OccurredAt`: fecha/hora funcional del evento.
- `IncidentId`: RCA asociado.
- `SourceSystem`, `ExternalTaskId`, `ExternalEventId`,
  `ExternalWorkOrderId`: correlacion externa.
- `PayloadJson`: serializacion de `RcaDomainEventDto`.
- `Status`: `Pending`, `Publishing`, `Published`, `Failed`, `DeadLetter`.
- `AttemptCount`: cantidad de intentos.
- `NextAttemptAt`: proxima fecha elegible para publicar.
- `LastAttemptAt`: ultimo intento.
- `PublishedAt`: fecha de publicacion exitosa.
- `LastError`: ultimo error resumido.
- Campos base existentes de tenant/auditoria heredados del patron de dominio.

Indices:

- Unico por `TenantId + EventId`.
- Busqueda por `TenantId + Status + NextAttemptAt`.
- Busqueda por `TenantId + IncidentId + OccurredAt`.
- Busqueda por `TenantId + EventType + OccurredAt`.

### Captura de Eventos

La primera implementacion debe crear eventos outbox desde los mismos puntos de
servicio que hoy permiten reconstruir el feed derivado:

- Creacion de incidente.
- Wizard step completado.
- Causa creada o marcada raiz.
- Accion creada o completada.
- Evidencia registrada.
- Fact operacional registrado.
- Intake externo creado, abierto, enviado, revisado, rechazado, revocado o
  expirado.
- RCA escalado a 8D o cerrado.

Cada evento debe usar el mismo envelope que el feed actual. Si no se puede
garantizar la captura para todos los eventos en el primer corte tecnico, el
endpoint actual debe seguir usando feed derivado y la documentacion debe marcar
el outbox como parcial hasta completar la cobertura.

### Lectura de Eventos

El endpoint actual puede evolucionar por fases:

1. Seguir devolviendo feed derivado mientras se llena outbox.
2. Agregar fallback: leer outbox cuando exista y derivado para compatibilidad
   historica.
3. Cambiar a outbox como fuente primaria cuando el smoke confirme cobertura.

No se debe cambiar el shape publico de `RcaDomainEventDto` en `/api/v1`.

### Publicador

Un servicio interno de publicacion debe:

- Tomar eventos `Pending` o `Failed` con `NextAttemptAt <= now`.
- Marcar temporalmente como `Publishing`.
- Publicar a destinos configurados.
- Marcar `Published` si todos los destinos obligatorios responden OK.
- Incrementar `AttemptCount`, guardar `LastError` y calcular backoff si falla.
- Pasar a `DeadLetter` cuando exceda el maximo de reintentos configurado.

La primera version puede ser manual o invocada por endpoint/admin/script. El
hosted service automatico puede quedar para un segundo corte si conviene
mantener bajo el riesgo operativo.

### Webhooks Configurables

Configuracion inicial sugerida en `appsettings`:

```json
{
  "RcaIntegration": {
    "Webhooks": [
      {
        "Name": "gantt-local",
        "Url": "https://example.local/rca/events",
        "Enabled": false,
        "Secret": "",
        "EventTypes": [ "RcaIncidentCreated", "RcaClosed" ]
      }
    ]
  }
}
```

Reglas:

- Webhooks deshabilitados por default.
- No persistir secretos en docs ni defaults reales.
- Firmar payloads con HMAC cuando `Secret` exista.
- Timeout corto por entrega.
- Reintento por outbox, no por bloqueo del request principal.

### Endpoints Operativos

Endpoints candidatos para fases P2 posteriores:

```http
GET  /api/v1/integrations/rca/outbox/status
POST /api/v1/integrations/rca/outbox/publish
GET  /api/v1/integrations/rca/outbox/dead-letter
POST /api/v1/integrations/rca/outbox/{id}/retry
```

Estos endpoints deben requerir roles administrativos o de integracion. No son
necesarios para el primer commit de la tabla outbox si la validacion se hace por
servicio/tests.

## Errores y Reintentos

- Fallos transitorios: volver a `Failed` con `NextAttemptAt` calculado por
  backoff.
- Fallos permanentes o maximos intentos: `DeadLetter`.
- Payload invalido: `DeadLetter` con `LastError` resumido.
- Consumidor caido: RCA sigue operando; el outbox acumula pendientes.

## Testing

Pruebas minimas para el primer corte outbox:

- Crear evento outbox con envelope compatible.
- Rechazar duplicado por `TenantId + EventId`.
- Listar pendientes ordenados por `NextAttemptAt`.
- Marcar publicado.
- Marcar fallo con incremento de intento y backoff.
- Mantener test existente de compatibilidad de eventos.

Smokes posteriores:

- Crear RCA por API y confirmar evento outbox persistido.
- Publicar contra receptor local falso y validar firma/headers.
- Simular fallo remoto y validar reintento/dead-letter.

## Criterios de Aceptacion

- El RCA no falla si no hay destinos configurados.
- El feed `/api/v1/integrations/rca/events` mantiene compatibilidad.
- Los eventos nuevos tienen idempotencia por `EventId`.
- Los errores de entrega quedan auditables.
- No hay dependencia directa a otros modulos.

## Plan de Implementacion Recomendado

1. Crear entidad/mapping/migracion `RcaOutboxEvent`.
2. Crear servicio de outbox con persistencia, deduplicacion y consultas de
   pendientes.
3. Registrar eventos outbox para un subconjunto inicial de alto valor:
   incidente creado, RCA cerrado, accion completada y fact registrado.
4. Ampliar cobertura hasta igualar el feed derivado.
5. Agregar publicador y configuracion de webhooks deshabilitada por default.
6. Agregar endpoints operativos solo cuando haya politica de roles definida.

## Riesgos

- Intentar reemplazar el feed derivado antes de capturar todos los eventos puede
  ocultar historial a consumidores.
- Publicar webhooks dentro del request principal puede degradar el modulo RCA.
- Configurar secretos reales en archivos versionados compromete seguridad.
- Exponer endpoints operativos sin roles claros abre superficie administrativa.
