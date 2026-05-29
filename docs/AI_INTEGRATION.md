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
```

## Modo Stub

La implementacion inicial usa `StubRcaAiGatewayClient`. Este modo no llama a ningun modelo externo: genera respuestas deterministicas por reglas simples para poder probar el flujo, validar contratos y continuar el desarrollo del modulo.

Configuracion actual:

```json
{
  "AiGateway": {
    "Mode": "Stub",
    "BaseUrl": "",
    "TimeoutSeconds": 30
  }
}
```

Cuando exista el AI Gateway compartido, se reemplaza la implementacion de `IRcaAiGatewayClient` por un cliente HTTP. Los controladores, contratos publicos y servicio de aplicacion no necesitan cambiar.

## Fallback

El modulo debe poder funcionar sin IA.

La IA es una capacidad asistida y opt-in por tenant o instalacion, no una dependencia obligatoria para operar el RCA.

