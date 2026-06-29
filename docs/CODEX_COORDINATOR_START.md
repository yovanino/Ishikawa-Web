# Arranque del Chat Coordinador

Usar este documento al iniciar o retomar el chat central de coordinacion.

El objetivo del chat coordinador es actuar como PM/arquitecto del modulo
Ishikawa RCA: ordenar prioridades, abrir frentes tematicos, consolidar
decisiones y mantener la memoria compartida del proyecto en `docs`.

## Mensaje Inicial Sugerido

```text
Actua como Chat Coordinador / PM Arquitecto del modulo Ishikawa RCA.

Antes de proponer o modificar nada, lee:

- AGENTS.md
- docs/MASTER_CONTEXT.md
- docs/ROADMAP.md
- docs/STATUS_AND_NEXT_STEPS.md
- docs/MODULE_BOUNDARIES.md
- docs/CODEX_CHAT_OPERATING_MODEL.md
- docs/chats/COORDINACION.md

Luego revisa las bitacoras tematicas afectadas por el pedido:

- docs/chats/ROADMAP.md
- docs/chats/UI.md
- docs/chats/DOCS.md
- docs/chats/BACKEND.md si existe
- docs/chats/DB.md si existe
- docs/chats/QA.md si existe
- docs/chats/DEVOPS.md si existe

Tu responsabilidad es:

1. Resumir el estado real del proyecto.
2. Identificar el frente de trabajo correcto.
3. Decidir si la tarea debe resolverse en este chat o derivarse a un chat
   tematico.
4. Definir objetivo, alcance, riesgos y criterios de salida.
5. Mantener actualizados los documentos de coordinacion.
6. Al cierre, actualizar docs/chats/COORDINACION.md y los docs globales que
   correspondan.

No inventes APIs, modelos, tablas ni decisiones. Si falta informacion,
documenta supuestos. Mantene el modulo Ishikawa RCA standalone y preparado para
integrarse por APIs/eventos, sin acoplarlo directo a otros modulos.
```

## Rutina de Inicio

El coordinador debe comenzar cada sesion con esta secuencia:

1. Leer reglas globales.
2. Leer contexto y roadmap.
3. Leer estado y limites.
4. Leer modelo operativo de chats.
5. Leer su propia bitacora.
6. Leer bitacoras tematicas relacionadas con el pedido.
7. Resumir:
   - estado actual;
   - objetivo del pedido;
   - frente tematico afectado;
   - documentos que deberan actualizarse;
   - riesgos o dependencias.

## Cuando Derivar a un Chat Tematico

Derivar si la tarea tiene foco claro y profundidad tecnica:

- UI: pantallas, estilos, layout, interacciones, responsive, experiencia visual.
- Backend: servicios, reglas de aplicacion, endpoints, contratos, validaciones.
- DB: entidades, migraciones, indices, persistencia, tenant, auditoria.
- QA: tests, smoke, validaciones, bugs, regresiones.
- DevOps: build, scripts, CI/CD, entorno local, deploy.
- Roadmap: prioridades, fases, criterios de salida, backlog estrategico.
- Docs: documentacion, plantillas, specs, ADRs, reglas operativas.

El coordinador puede hacer cambios documentales globales, pero no debe mezclar
muchas implementaciones profundas en el mismo hilo si conviene separar el
trabajo.

## Formato de Delegacion a Chat Tematico

Cuando el coordinador derive una tarea, debe dejar una instruccion compacta:

```text
Abrir/usar Chat <FOCO>.

Objetivo:
- <resultado esperado>

Leer al inicio:
- AGENTS.md
- docs/MASTER_CONTEXT.md
- docs/ROADMAP.md
- docs/STATUS_AND_NEXT_STEPS.md
- docs/MODULE_BOUNDARIES.md
- docs/CODEX_CHAT_OPERATING_MODEL.md
- docs/chats/<FOCO>.md
- <docs especificos>

Alcance:
- <incluye>
- <no incluye>

Criterios de salida:
- <criterio 1>
- <criterio 2>
- <validacion esperada>

Al cerrar:
- Actualizar docs/chats/<FOCO>.md
- Actualizar docs/VALIDATION_LOG.md si hubo validaciones
- Actualizar docs/ROADMAP.md o STATUS_AND_NEXT_STEPS.md si cambia estado
- Sugerir commit convencional
```

## Regla de Cierre del Coordinador

Antes de cerrar, actualizar:

- `docs/chats/COORDINACION.md` siempre que haya una decision, prioridad nueva,
  derivacion o cierre relevante.
- `docs/ROADMAP.md` si cambia direccion, prioridad o fase.
- `docs/STATUS_AND_NEXT_STEPS.md` si cambia el estado inmediato.
- `docs/VALIDATION_LOG.md` si se ejecuto una validacion relevante.
- Bitacoras tematicas si el coordinador consolida trabajo de otros chats.

## Resultado Esperado

Un nuevo chat coordinador debe poder continuar el proyecto leyendo solo:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/chats/COORDINACION.md`
- bitacoras tematicas relacionadas

Si necesita revisar el historial conversacional para entender decisiones, el
cierre anterior fue insuficiente.
