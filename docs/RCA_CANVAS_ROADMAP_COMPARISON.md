# Comparacion contra Roadmap RCA Canvas Industrial

Fecha de analisis: 4 de junio de 2026

Documento base: `docs/Roadmap_RCA_Canvas_Industrial.docx`

## Lectura principal

El roadmap define que el modulo no debe quedarse en un diagrama de Ishikawa aislado. La direccion correcta es un **RCA Investigation Canvas Industrial**: un espacio operacional donde el incidente, los hechos, las causas, la evidencia, los 5 Whys, las acciones correctivas, el cierre y el aprendizaje historico conviven en una misma experiencia.

La diferenciacion frente a herramientas mainstream no esta solo en dibujar causas, sino en conectar el RCA con la operacion industrial: Gantt, SCADA, Andon, TPM, OEE, eventos externos, evidencia real y futura IA asistida.

## Estado verificado en esta carpeta

Ruta analizada: `C:\Users\jprivamonti\Documents\Ishikawa Web`

El repo actual contiene una base fundacional:

- Solucion ASP.NET Core MVC en .NET 9.
- Home conceptual del modulo Ishikawa RCA.
- Documentacion inicial de contexto, contratos API, integracion AI y limites modulares.
- Remote GitHub configurado hacia `https://github.com/yovanino/Ishikawa-Web.git`.

Todavia no se observan en esta carpeta entidades persistentes RCA, servicios de dominio, migraciones EF Core, APIs operativas, canvas editable, evidencia, acciones, wizard real ni exportacion.

## Comparacion por fase

| Fase del roadmap | Objetivo | Estado en esta carpeta | Gap principal |
| --- | --- | --- | --- |
| Fase 0 - Base | Repositorio, vision, contratos, limites modulares | Avanzada | Falta convertir la vision en backlog versionado por entregables |
| Fase 1 - MVP operacional | RCA real de punta a punta | Pendiente | Dominio, persistencia, CRUD, wizard, API v1, estados |
| Fase 2 - Canvas visual | Herramienta visual de investigacion | Pendiente | Ishikawa editable, causas/subcausas, panel lateral, layout |
| Fase 3 - Evidencia y acciones | Cierre de mejora | Pendiente | Adjuntos, notas, CAPA, responsables, fechas, cierre, reporte |
| Fase 4 - Integracion industrial | Conectar RCA con operacion | Documentado, no implementado | Entradas/salidas via API/eventos, snapshots, timeline |
| Fase 5 - IA asistida | Acelerar investigacion con control humano | Arquitectura prevista | AI Gateway, sugerencias, resumen, recurrencia, 8D |
| Fase 6 - Plataforma | Escalar a producto empresarial | Vision documentada | Roles, auditoria, tenant, dashboards, metricas historicas |

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

La brecha actual no es de estetica solamente. El salto necesario es pasar de una base conceptual MVC a un producto operativo trazable.

Prioridad recomendada:

1. Crear el modelo de dominio RCA minimo: incidente, categoria, causa, subcausa, accion, evidencia, comentario y estado.
2. Agregar persistencia EF Core/MySQL y migracion inicial.
3. Implementar CRUD de incidentes y detalle operativo.
4. Implementar wizard basico para problema, impacto, severidad, contexto y alcance.
5. Crear API v1 para alta/consulta de incidentes desde sistemas externos.
6. Preparar contrato de canvas para categorias, causas y subcausas.
7. Luego avanzar al canvas visual con panel lateral y 5 Whys por causa.

## Riesgos a controlar

- Construir una UI visual antes de tener trazabilidad y dominio persistente.
- Agregar IA antes de contar con datos estructurados confiables.
- Intentar integrar todos los modulos externos antes de validar el RCA manual.
- No definir estados, auditoria y permisos desde temprano.
- Copiar un QMS generico y perder el foco industrial operacional.

## Decision propuesta

Usar este roadmap como documento rector y comenzar por **Fase 1 - MVP operacional** en esta carpeta, manteniendo el canvas visual como norte inmediato de Fase 2.

Cada avance deberia cerrarse con:

- Validacion local.
- Commit descriptivo.
- Push a GitHub.
- Actualizacion de esta comparacion o del roadmap operativo si cambia el alcance.
