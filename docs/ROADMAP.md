# Roadmap del Modulo Ishikawa RCA

## Estado Actual

El repositorio ya tiene la base modular, persistencia MySQL, flujo RCA inicial, contratos de integracion y stub de AI Gateway. El siguiente objetivo es aplicar la migracion en un MySQL real y validar el smoke test end-to-end.

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
- [ ] Subcausas.
- [x] Acciones correctivas.
- [x] Persistencia en MySQL.
- [x] API para crear incidentes desde sistemas externos.
- [x] Contexto basico de reclamo interno/externo.
- [x] Actor de reclamo explicito: area interna, cliente o proveedor.

## Fase 2 - Experiencia Visual

- Drag and drop.
- Zoom y pan.
- Panel lateral de edicion.
- Context menu industrial.
- Auto-layout inicial.
- Evidencias y adjuntos.
- Comentarios.

## Fase 3 - Integracion Operacional

- SignalR para actualizaciones en vivo.
- Webhooks/eventos salientes.
- Integracion Gantt por API.
- Integracion SCADA/Gateway por API.
- Timeline de RCA.
- Estados y auditoria ampliada.
- Intake link seguro para proveedor/cliente.

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

