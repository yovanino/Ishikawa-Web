# Roadmap del Modulo Ishikawa RCA

## Estado Actual

El repositorio ya tiene la base modular, persistencia MySQL, flujo RCA inicial, evidencias RCA por metadatos/referencia, contratos de integracion y stub de AI Gateway. El siguiente objetivo es seguir endureciendo workflow, permisos, auditoria y almacenamiento documental real.

## Fase 0 - Base del Repositorio

- [x] Documentar alcance modular.
- [x] Definir contratos API iniciales.
- [x] Definir eventos de dominio.
- [x] Definir preparacion para AI Gateway.
- [x] Crear solucion ASP.NET Core MVC.

## Fase 1 - MVP Operacional

- [x] CRUD de incidentes RCA.
- [ ] Wizard basico.
- [x] Canvas Ishikawa visual inicial.
- [x] Categorias dinamicas base.
- [x] Causas.
- [x] Subcausas.
- [x] Acciones correctivas.
- [x] Cierre/validacion basica de acciones correctivas.
- [x] Escalamiento a 8D.
- [x] Cierre formal de RCA.
- [x] Evidencias RCA iniciales por metadatos y referencia.
- [x] Persistencia en MySQL.
- [x] API para crear incidentes desde sistemas externos.
- [x] Contexto basico de reclamo interno/externo.
- [x] Actor de reclamo explicito: area interna, cliente o proveedor.
- [x] Intake link seguro MVP para proveedor/cliente.
- [x] Revision interna de intake externo con importacion a causa/accion.

## Fase 2 - Experiencia Visual

- Drag and drop.
- Zoom y pan.
- Panel lateral de edicion.
- Context menu industrial.
- Auto-layout inicial.
- [x] Evidencias iniciales con tipo, fuente, resumen y referencia.
- Adjuntos binarios/documentales.
- Comentarios.

## Fase 3 - Integracion Operacional

- SignalR para actualizaciones en vivo.
- Webhooks/eventos salientes.
- Integracion Gantt por API.
- Integracion SCADA/Gateway por API.
- [x] Timeline visual de RCA alimentada por eventos de integracion.
- Estados y auditoria ampliada.
- [x] Evento de RCA escalado a 8D.
- [x] Evento de RCA cerrado.
- [x] Evento de accion correctiva completada.
- Eventos de integracion para intake externo.
- [x] Rechazo formal de intake externo con motivo.
- Auditoria ampliada de intake externo.
- [x] Evidencia RCA inicial vinculable a causa o intake externo.
- Adjuntos binarios/documentales en intake externo.

## Fase 4 - Inteligencia Asistida

- Consumo de AI Gateway.
- Sugerencia de causas.
- Sugerencia de acciones.
- Deteccion de recurrencia.
- Resumen del analisis.
- Borrador de 8D.

## Fase 5 - Plataforma Global

- Registro en app global.
- Integracion con Identity/Tenant global.
- Integracion con maestros globales de clientes y proveedores.
- Integracion con Event Bus global.
- Timeline operacional unificado.
- Dashboard cross-module.

