# Chat UI

## Alcance

Evolucionar la experiencia visual del modulo Ishikawa RCA: pantallas MVC,
canvas Ishikawa, wizard, timeline, evidencias, CAPA, estados visuales,
interacciones y contenido de interfaz.

## Regla de Inicio

Leer antes de trabajar:

- `AGENTS.md`
- `docs/MASTER_CONTEXT.md`
- `docs/ROADMAP.md`
- `docs/STATUS_AND_NEXT_STEPS.md`
- `docs/MODULE_BOUNDARIES.md`
- `docs/CODEX_CHAT_OPERATING_MODEL.md`
- `docs/UI_CONTENT_BENCHMARK.md`
- `docs/chats/UI.md`

Despues de leer, resumir:

- Estado actual de UI.
- Objetivo de la tarea.
- Pantallas o componentes afectados.
- Riesgos de UX, responsive, accesibilidad o regresion funcional.
- Validaciones previstas.

## Estado Actual

La UI ya tiene una base operativa para alta, listado, detalle RCA, canvas
Ishikawa inicial, wizard guiado, timeline unificado, evidencias con previews,
acciones correctivas, cierre formal y exportacion PDF.

La siguiente mejora visual natural es avanzar hacia una experiencia de cockpit
industrial: comandos compactos, estados claros, board visual, timeline mas
accionable y paneles por rol.

## Analisis UI del Proyecto

Fecha de analisis: 2026-06-06.

### Superficie Web Detectada

- La UI esta implementada como ASP.NET Core MVC server-rendered en
  `src/IshikawaRca.Web`.
- La navegacion principal esta en `Views/Shared/_Layout.cshtml`, con marca
  `Ishikawa RCA`, tabs `Cockpit` e `Incidentes`, y estado superior
  `Standalone` / `AI Stub`.
- La pantalla inicial `Views/Home/Index.cshtml` funciona como cockpit de
  entrada y presenta capacidades del modulo: API v1, canvas 6M, AI Gateway
  stub y snapshots externos.
- El tablero `Views/Rca/Index.cshtml` lista incidentes con metricas de total,
  abiertos, alta prioridad y reclamos de proveedor, mas tabla operativa con
  problema, reclamo, sistema, severidad, estado, activo y fecha.
- La pantalla `Views/Rca/Create.cshtml` permite alta manual de RCA con
  problema, severidad, actor del reclamo, fecha/hora, maquina, linea, orden de
  trabajo y reportado por.
- La pantalla externa `Views/ExternalIntake/Index.cshtml` usa layout aislado,
  sin navegacion completa, para respuesta limitada de cliente/proveedor.
- El estilo visual principal vive en `wwwroot/css/site.css`, con Bootstrap,
  jQuery y validacion unobtrusive como base tecnica.

### Detalle RCA

`Views/Rca/Details.cshtml` concentra la mayor parte del producto UI. La vista
incluye:

- Command center del incidente con origen, actor, titulo, descripcion, estado,
  exportacion PDF y retorno al tablero.
- KPI rail de severidad, reclamo, causas, evidencias, acciones abiertas y
  causa raiz.
- Wizard RCA con etapas `Problem`, `Causes`, `Evidence`, `Actions`,
  `Validation` y `Closed`, porcentaje, checklist, metricas y bloqueos.
- Paneles de estado operativo y contexto industrial.
- Formularios de escalamiento 8D y cierre formal.
- Linea de hechos con contexto industrial, links a causa/evidencia/accion/
  intake y correlacion externa.
- Timeline unificado de eventos RCA, evidencias, hechos, acciones, wizard e
  intake externo.
- Gestion visual de evidencias con tarjetas, chips de validacion, tags,
  previews por tipo de archivo, descarga, edicion, reemplazo y eliminacion.
- Intake externo con creacion de link, revocacion, revision, importacion a RCA
  y rechazo formal.
- Canvas Ishikawa con ramas 6M, tarjetas de causa, puntajes y marcado de causa
  raiz.
- Resolucion del problema separada entre causa raiz del defecto y FUGA/no
  deteccion.
- Formulario de nuevas acciones con tipo correctiva/preventiva/preventiva de
  recurrencia y ambito de resolucion.

### View Models y Flujo MVC

- `RcaIncidentDetailsViewModel` agrupa todo lo que necesita el detalle: incidente,
  canvas, acciones, intakes externos, evidencias, facts, timeline, wizard y
  formularios embebidos.
- `RcaController` arma el detalle con `BuildDetailsViewModelAsync`, consultando
  incidente, canvas, facts, acciones, evidencias, intakes externos, eventos de
  integracion y progreso wizard.
- Los formularios usan POST MVC con antiforgery para crear causas, acciones,
  facts, evidencias, links externos, revisar/rechazar intake, avanzar wizard,
  escalar 8D y cerrar RCA.
- El controlador usa `DemoTenantId` para altas desde UI; esto no es deuda
  puramente visual, pero impacta toda experiencia UI hasta que exista tenant
  real.

### Estilo y Componentes CSS Actuales

- La direccion visual ya es industrial premium: superficies blancas, bordes
  suaves, sombras sobrias, densidad media y paleta con estados verde/ambar/rojo/
  azul/gris.
- Existen primitivas CSS reutilizables: `metric-tile`, `status-chip`,
  `severity-pill`, `validation-chip`, `analysis-panel`, `detail-panel`,
  `fishbone-board`, `branch-lane`, `evidence-card`, `timeline-item-card`,
  `wizard-step`, `wizard-check-card`, `action-row` y `empty-state`.
- Hay responsive basico para grillas principales, wizard, forms, evidencias,
  fishbone y columnas de acciones mediante breakpoints `1100px`, `900px` y
  `560px`.
- La UI todavia depende de formularios tradicionales y recarga de pagina; no
  hay interacciones tipo drag/drop, zoom/pan, filtro live, panel lateral o
  actualizaciones en vivo.

### Brechas UI Frente al Roadmap P1

- Falta convertir el command center en command bar persistente con severidad,
  SLA, linea, maquina, responsable, fase actual y acciones primarias compactas.
- El KPI rail existe, pero aun no muestra edad de contencion, riesgo de
  recurrencia, acciones vencidas ni indicadores de SLA.
- El fishbone es visual y organizado por ramas, pero no interactivo: no hay
  drag/reorder, zoom, pan, fit-to-screen, auto-layout ni menu contextual.
- Las tarjetas de causa no muestran todavia una semantica completa de
  evidencia asociada, confianza, validacion, owner o sugerencia IA.
- Las acciones ya estan separadas por causa raiz y FUGA, pero falta un CAPA
  board operacional por correctiva, preventiva y recurrencia con estados,
  vencimientos y responsables escaneables.
- El timeline unificado existe, pero falta filtrado por tipo, severidad, fuente
  o referencia operacional.
- No hay vistas por rol, modo tablet especifico, modo cockpit/Obeya ni estados
  offline/degradado.
- El AI Gateway tiene endpoints/stub, pero no hay panel UI para sugerencias con
  aprobacion humana.

### Riesgos UI Detectados

- `Details.cshtml` es una vista monolitica muy grande; cualquier mejora de UI
  puede provocar regresiones si no se valida detalle, formularios embebidos y
  responsive.
- La densidad del detalle es alta y util, pero varias acciones compiten en una
  misma pagina; conviene introducir jerarquia progresiva antes de sumar mas
  bloques.
- Los formularios embebidos en panels sticky son practicos en desktop, pero
  necesitan validacion tablet/mobile antes de un piloto industrial.
- La falta de auth/roles limita cualquier intento serio de vistas por rol o
  acciones sensibles contextualizadas.
- La UI externa de intake es correctamente aislada, pero todavia no contempla
  adjuntos binarios/documentales externos.

### Proximo Corte UI Recomendado

Mantener el alcance standalone y atacar primero una mejora P1 incremental sobre
detalle RCA:

1. Refactor visual del detalle en cockpit de trabajo: command bar + KPI rail
   mejorado + jerarquia de panels.
2. CAPA board separado por correctiva, preventiva y recurrencia, reutilizando
   las acciones existentes y sin cambiar contratos.
3. Timeline filtrable por tipo/fuente/severidad usando los datos ya presentes
   en `UnifiedTimeline`.
4. Fishbone con tarjetas de causa mas informativas antes de implementar drag,
   zoom o pan.
5. Validacion responsive/tablet del detalle, evidencia y formularios sticky.

## Decisiones

- La UI debe sentirse como herramienta operacional industrial, no como landing
  page ni formulario generico.
- El canvas Ishikawa debe evolucionar como artefacto vivo con causas,
  evidencias, validacion, acciones y trazabilidad.
- Toda mejora UI debe respetar el alcance standalone del modulo y usar
  integraciones futuras por API/eventos, no por acoplamiento directo.

## Cambios Realizados

- Se crea esta bitacora como punto de conexion para futuros chats UI.
- 2026-06-06: se abre/usa el chat tematico UI y se confirma lectura del
  contexto obligatorio del repositorio para orientar trabajos de interfaz.
- 2026-06-06: se analiza el proyecto y se vuelca en esta bitacora solo el
  inventario y diagnostico relacionado con UI MVC, detalle RCA, estilos,
  brechas P1 y riesgos de experiencia.
- 2026-06-11: se inicia P1 sobre el detalle RCA con command bar industrial:
  origen, actor, fecha de creacion, severidad, estado, progreso RCA, linea,
  maquina, OT, fase actual, owner y accesos compactos a PDF/CAPA/volver.
- 2026-06-11: se amplia el KPI rail del detalle con acciones vencidas,
  proximo vencimiento, edad de contencion y riesgo de recurrencia calculado
  desde causa raiz y acciones abiertas/vencidas.
- 2026-06-11: se agrega CAPA board en la resolucion del problema, separado en
  carriles correctiva, preventiva y recurrencia, reutilizando acciones
  existentes y conservando abajo la gestion por causa raiz/FUGA.
- 2026-06-11: se enriquecen las tarjetas de causa del fishbone con puntajes
  P/I/F, score total, causa padre, marca de raiz y conteos de evidencias,
  hechos y acciones vinculadas.
- 2026-06-11: se agrega filtro client-side al timeline unificado por todos,
  hechos, evidencias, acciones, wizard y eventos externos.
- 2026-06-11: se agrega toolbar client-side al fishbone para zoom out, fit,
  zoom in y paneo por arrastre del tablero.
- 2026-06-11: se agrega panel lateral contextual de solo lectura para abrir
  detalle de causas, evidencias y acciones desde sus tarjetas.

## Pendientes

- Continuar el corte P1 con drag/reorder de causas, edicion avanzada desde
  panel lateral, empty/loading/error/offline y luego responsive/tablet.
- Mantener sincronizado este archivo luego de cada trabajo UI.
- Registrar validaciones visuales y funcionales relevantes en
  `docs/VALIDATION_LOG.md`.
- Validar responsive/tablet del detalle RCA antes de ampliar interacciones.
- Evaluar extraccion progresiva de secciones del detalle a parciales o
  componentes MVC para reducir riesgo de cambios sobre una vista monolitica.

## Riesgos

- Cambios visuales pueden romper flujos MVC existentes si no se valida alta,
  detalle, wizard, evidencias, acciones y cierre.
- El exceso de decoracion puede reducir claridad operacional.
- Nuevos estados visuales deben ser consistentes con la gramatica industrial
  definida en `docs/UI_CONTENT_BENCHMARK.md`.

## Validaciones

- 2026-06-06: lectura documental inicial completada; no se ejecutaron
  validaciones tecnicas porque no hubo cambios de codigo ni UI funcional.
- 2026-06-06: analisis estatico de UI completado mediante lectura de vistas,
  view models, controlador MVC y CSS; no se ejecuto build ni smoke porque no
  hubo cambios funcionales.
- 2026-06-11: `dotnet build IshikawaRca.sln /m:1` paso con 0 warnings y 0
  errores.
- 2026-06-11: `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso ejecutado en serie. La validacion visual completa queda diferida al
  cierre del bloque cockpit/tablet para evitar levantar app + DB en cada
  micro-ajuste.
- 2026-06-11: para el CAPA board, `dotnet build IshikawaRca.sln /m:1` paso con
  0 warnings y 0 errores, y `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso en serie.
- 2026-06-11: para fishbone cause cards, `dotnet build IshikawaRca.sln /m:1`
  paso con 0 warnings y 0 errores, y `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso en serie.
- 2026-06-11: para timeline filtrable, `dotnet build IshikawaRca.sln /m:1`
  paso con 0 warnings y 0 errores, y `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso en serie.
- 2026-06-11: para zoom/pan del fishbone, `dotnet build IshikawaRca.sln /m:1`
  paso con 0 warnings y 0 errores, y `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso en serie.
- 2026-06-11: para panel lateral contextual, `dotnet build IshikawaRca.sln /m:1`
  paso con 0 warnings y 0 errores, y `dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj`
  paso en serie.

## Ultimo Cierre

- Fecha: 2026-06-11.
- Resumen: P1 avanza con command bar/KPI rail, CAPA board, tarjetas de causa,
  zoom/pan, timeline filtrable y panel lateral contextual en el detalle RCA.
- Commit: pendiente en este cierre.
