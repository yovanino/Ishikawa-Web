# Integracion con IA

La IA no debe vivir embebida dentro del modulo Ishikawa RCA. Debe consumirse mediante un AI Gateway compartido por todos los modulos de la plataforma industrial.

## Arquitectura

```text
Ishikawa RCA
    |
    | REST API / SDK interno futuro
    v
AI Gateway
    |
    v
IA Local / IA Cloud / Modelos especializados
```

## Servidor IA Dedicado

La IA local puede instalarse en un servidor dedicado usando herramientas como:

- Ollama.
- llama.cpp.
- LocalAI.
- vLLM.
- Modelos locales como Qwen, Mistral, DeepSeek o similares.

El modulo Ishikawa no debe saber que motor especifico ejecuta la inferencia. Solo debe consumir el contrato del AI Gateway.

## Casos de Uso para Ishikawa

- Sugerir causas posibles.
- Ordenar causas por probabilidad.
- Recomendar acciones correctivas.
- Resumir historial del problema.
- Detectar recurrencia.
- Comparar contra RCA anteriores.
- Convertir notas de operador en estructura Ishikawa.
- Generar borrador de 8D.

## Regla de Seguridad

La IA no ejecuta acciones industriales directamente.

Puede proponer, resumir, clasificar o redactar. La ejecucion debe quedar bajo aprobacion humana, regla auditable o workflow autorizado.

## Endpoints Esperados del AI Gateway

```http
POST /ai/rca/suggest-causes
POST /ai/rca/suggest-actions
POST /ai/rca/summarize
POST /ai/rca/detect-recurrence
POST /ai/rca/generate-8d-draft
```

## Endpoints del Modulo Ishikawa

El modulo expone endpoints propios para que la UI o integraciones pidan asistencia. Internamente arma el contexto RCA y lo envia al cliente AI Gateway configurado.

```http
POST /api/v1/rca/incidents/{id}/ai/suggest-causes
POST /api/v1/rca/incidents/{id}/ai/suggest-actions
POST /api/v1/rca/incidents/{id}/ai/summarize
POST /api/v1/rca/incidents/{id}/ai/detect-recurrence
POST /api/v1/rca/incidents/{id}/ai/generate-8d-draft
```

## Modo Stub

La implementacion inicial usa `StubRcaAiGatewayClient`. Este modo no llama a ningun modelo externo: genera respuestas deterministicas por reglas simples para poder probar el flujo, validar contratos y continuar el desarrollo del modulo.

Configuracion actual:

```json
{
  "AiGateway": {
    "Mode": "Stub",
    "BaseUrl": "",
    "TimeoutSeconds": 30,
    "ApiKey": "",
    "UseFallbackOnFailure": true
  }
}
```

## Base HTTP disponible

Desde el ajuste P3 del 2026-06-12 existe la base `HttpRcaAiGatewayClient`.

- Publica el `RcaAiContextDto` por JSON a `/ai/rca/suggest-causes`,
  `/ai/rca/suggest-actions`, `/ai/rca/summarize`,
  `/ai/rca/detect-recurrence` y `/ai/rca/generate-8d-draft`.
- Usa `AiGateway:BaseUrl` como URL absoluta.
- Envia `Authorization: Bearer <ApiKey>` cuando `AiGateway:ApiKey` tiene valor.
- Aplica timeout por request usando `AiGateway:TimeoutSeconds`.
- Devuelve fallos controlados:
  `AI_GATEWAY_CONFIGURATION_INVALID`, `AI_GATEWAY_UNAVAILABLE` y
  `AI_GATEWAY_INVALID_RESPONSE`.

## Estado actual P3 Task 3

Desde el cierre de Task 3 del 2026-06-13, el modulo ya expone y enruta por
gateway las sugerencias de deteccion de recurrencia y borrador 8D.

- `RcaAiController` publica `detect-recurrence` y `generate-8d-draft`.
- `RcaAiAssistantService` arma el `RcaAiContextDto` y delega ambas operaciones
  al gateway configurado.
- `StubRcaAiGatewayClient` devuelve respuestas deterministicas con
  `metadata.isFallback = true` para validar el flujo standalone.
- `ConfiguredRcaAiGatewayClient` aplica el mismo enrutamiento
  `Stub`/`Http` + fallback opcional que ya usaban causas, acciones y resumen.

## Seleccion de modo y fallback

Desde la Task 2 del 2026-06-12 el runtime ya no queda fijado al stub.

- `AiGateway:Mode = "Stub"` ejecuta `StubRcaAiGatewayClient` de forma directa.
- `AiGateway:Mode = "Http"` ejecuta `HttpRcaAiGatewayClient`.
- Si `Mode = "Http"` y la llamada HTTP falla, `UseFallbackOnFailure = true`
  hace fallback al stub.
- Si `Mode = "Http"` y `UseFallbackOnFailure = false`, el modulo devuelve la
  falla original del gateway HTTP.

La seleccion se resuelve en `ConfiguredRcaAiGatewayClient`, sin cambiar
controladores, contratos publicos ni `RcaAiAssistantService`.

## Fallback

El modulo debe poder funcionar sin IA.

La IA es una capacidad asistida y opt-in por tenant o instalacion, no una dependencia obligatoria para operar el RCA.

