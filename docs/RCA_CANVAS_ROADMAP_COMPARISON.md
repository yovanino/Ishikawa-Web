# Comparacion contra Roadmap RCA Canvas Industrial

Fecha de analisis: 4 de junio de 2026

Documento base: `docs/Roadmap_RCA_Canvas_Industrial.docx`

## Lectura principal

El roadmap define que el modulo no debe quedarse en un diagrama de Ishikawa aislado. La direccion correcta es un **RCA Investigation Canvas Industrial**: un espacio operacional donde el incidente, los hechos, las causas, la evidencia, los 5 Whys, las acciones correctivas, el cierre y el aprendizaje historico conviven en una misma experiencia.

La diferenciacion frente a herramientas mainstream no esta solo en dibujar causas, sino en conectar el RCA con la operacion industrial: Gantt, SCADA, Andon, TPM, OEE, eventos externos, evidencia real y futura IA asistida.

## Estado verificado en esta carpeta

Ruta analizada: `C:\Users\jprivamonti\Documents\Ishikawa Web`

El repo actual, luego de sincronizar con `origin/main`, ya supera la base fundacional:

- Solucion modular ASP.NET Core MVC en .NET 9.
- Proyectos separados de dominio, contratos, aplicacion, infraestructura y web.
- Entidades persistentes RCA para incidente, ramas Ishikawa, causas, acciones correctivas, evidencias e intake externo.
- EF Core/MySQL con migraciones.
- Servicios de infraestructura y contratos API para incidentes, integraciones y asistencia IA.
- Vistas MVC operativas para lista, alta, detalle, wizard, evidencias, acciones, intake externo y cierre.
- Evidencias con adjuntos, previews, metadatos de validacion y ruta configurable.
- Exportacion PDF del RCA.
- Documentacion inicial de contexto, contratos API, integracion AI, limites modulares y estado.
- Remote GitHub configurado hacia `https://github.com/yovanino/Ishikawa-Web.git`.

La brecha actual ya no es la persistencia basica. La brecha fuerte esta en transformar el detalle operativo en un canvas de investigacion visual, con linea de hechos, edicion lateral de causas, 5 Whys por causa, estados metodologicos y cierre CAPA mas robusto.

## Comparacion por fase

| Fase del roadmap | Objetivo | Estado en esta carpeta | Gap principal |
| --- | --- | --- | --- |
| Fase 0 - Base | Repositorio, vision, contratos, limites modulares | Completa | Mantener documentacion viva |
| Fase 1 - MVP operacional | RCA real de punta a punta | Mayormente completa | Ajustar bordes de UX, validaciones y estados finos |
| Fase 2 - Canvas visual | Herramienta visual de investigacion | Parcial | Canvas editable real, panel lateral, layout estable, zoom/pan |
| Fase 3 - Evidencia y acciones | Cierre de mejora | Parcial alto | CAPA, eficacia, aprobaciones, timeline y relacion causa-evidencia mas visual |
| Fase 4 - Integracion industrial | Conectar RCA con operacion | Base preparada | Webhooks/eventos, snapshots reales y conectores con Gantt/SCADA/Andon/TPM/OEE |
| Fase 5 - IA asistida | Acelerar investigacion con control humano | Arquitectura prevista | AI Gateway real, sugerencias auditables, resumen, recurrencia y 8D |
| Fase 6 - Plataforma | Escalar a producto empresarial | Vision documentada | Roles, auditoria avanzada, tenant, dashboards y metricas historicas |

## Donde queremos ir

El destino de producto debe ser una pantalla de investigacion industrial completa:

- Cabecera del incidente con severidad, fuente, linea, maquina, responsable y estado.
- Linea de hechos con eventos, alarmas, mediciones, notas y evidencias ordenadas por tiempo.
- Canvas Ishikawa visual con categorias configurables, causas y subcausas.
- Panel lateral para editar causa, 5 Whys, evidencia, validacion, probabilidad y comentarios.
- Acciones correctivas/preventivas conectadas a causa raiz y con seguimiento CAPA.
- Reporte exportable y cierre formal.
- Integracion por APIs/eventos con modulos externos.
- IA asistida como borrador auditable, nunca como decision automatica.

## Brecha mas importante

La brecha actual no es de estetica solamente. El salto necesario es pasar de una operacion RCA funcional a un **canvas metodologico premium** que guie la investigacion como producto industrial.

Prioridad recomendada:

1. Agregar linea de hechos del RCA: eventos, alarmas, mediciones, notas y evidencia ordenada por tiempo.
2. Incorporar 5 Whys por causa, no solo a nivel incidente.
3. Agregar estado metodologico de causa: hipotesis, validada, rechazada, causa raiz y contribuyente.
4. Redisenar el detalle como investigacion en tres zonas: hechos/evidencia, canvas central y panel lateral.
5. Permitir edicion profunda de causa desde panel lateral.
6. Hacer visible la relacion causa-evidencia dentro del canvas.
7. Fortalecer CAPA: responsable, vencimiento, eficacia, aprobacion y cierre.
8. Preparar eventos salientes/API para que Gantt, SCADA, Andon, TPM y OEE puedan consumir estado RCA.

## Riesgos a controlar

- Construir una UI visual antes de tener trazabilidad y dominio persistente.
- Agregar IA antes de contar con datos estructurados confiables.
- Intentar integrar todos los modulos externos antes de validar el RCA manual.
- No definir estados, auditoria y permisos desde temprano.
- Copiar un QMS generico y perder el foco industrial operacional.

## Decision propuesta

Usar este roadmap como documento rector y avanzar con **Canvas Industrial Fase 2**, cerrando en paralelo las piezas de Fase 3 que hacen trazable la investigacion: hechos, 5 Whys, evidencia ligada a causa, CAPA y cierre.

Cada avance deberia cerrarse con:

- Validacion local.
- Commit descriptivo.
- Push a GitHub.
- Actualizacion de esta comparacion o del roadmap operativo si cambia el alcance.
