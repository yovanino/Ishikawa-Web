# Faltantes

Fecha de corte: 2026-06-18.

Este documento consolida los pendientes reales del modulo Ishikawa RCA despues
del cierre de P0, P1, P2, P3 y P4 standalone.

## Estado General

- P0 standalone: cerrado.
- P1 cockpit visual standalone: cerrado.
- P2 integracion operacional standalone: cerrado.
- P3 IA asistida gobernada standalone: cerrado.
- P4 gobierno documental y plataforma-ready standalone: cerrado.
- Rama actual: `main`, con commits locales pendientes de push.

El modulo ya puede operar como RCA standalone pilotable: crea, analiza, guia,
cierra, audita, expone APIs/eventos, versiona PDF de cierre, gobierna
documentos y ofrece contratos de discovery/resumen para consumidores externos.

## Pendientes Accionables Locales

Estos puntos se pueden trabajar dentro de este repositorio si se decide abrir
un nuevo corte.

1. Tests MVC/UI formales.
   - Cobertura automatizada de vistas criticas.
   - Flujos de detalle RCA, paneles de revision, evidencias, cierre y
     documentos.
   - Validacion visual completa con app y DB levantadas solo cuando convenga
     por costo/tiempo.

2. Suite formal ampliada.
   - Separar o estructurar mejor la suite liviana actual.
   - Agregar tests unitarios/integracion mas especificos por capa.
   - Mantener timeouts explicitos para evitar procesos colgados.

3. Reapertura gobernada de RCA.
   - Requiere regla de negocio formal: quien puede reabrir, en que estados,
     con que motivo y que auditoria genera.

4. Reportes corporativos de auditoria dentro del modulo.
   - Requiere definir consumidor y formato.
   - Puede partir de `rca_audit_records`, snapshots y documentos de cierre.

5. Adjuntos binarios/documentales en intake externo.
   - Permitir que clientes/proveedores adjunten evidencia limitada.
   - Requiere politica de tipos/tamanos, revision interna y storage.

6. Hardening adicional de adjuntos.
   - Validacion por firma/content sniffing.
   - Reglas productivas de tipos MIME.
   - Politica antivirus/DLP solo si se define proveedor.

7. Persistencia de orden de causas.
   - P1 ya permite drag/reorder visual.
   - Falta contrato de persistencia y regla de ordenamiento.

8. Edicion avanzada desde panel lateral.
   - Requiere definir que campos se editan inline y que validaciones aplican.

9. SLA visual del incidente.
   - Requiere regla formal de SLA por severidad, fuente, area o tipo de
     problema.

10. Context menu industrial.
    - Acciones rapidas sobre causa/evidencia/accion.
    - Debe respetar permisos y antiforgery.

11. Auto-layout del fishbone.
    - Mejora visual/ergonomica.
    - No debe cambiar contratos backend salvo que se persista layout.

12. Comentarios colaborativos.
    - Requiere definir modelo, permisos, menciones y auditoria.

13. Vistas por rol.
    - Ajustar experiencia para operador, calidad, supervisor, mantenimiento y
      administrador.

14. Modo cockpit/Obeya.
    - Vista de supervision ampliada sobre RCA actuales.
    - Puede consumir el summary existente, pero requiere diseno UI propio.

15. CI/CD.
    - Pipeline de build/test/lint/smoke.
    - Requiere definir runner, secretos y entorno DB.

## Pendientes Bloqueados por Plataforma Externa

Estos puntos no conviene implementarlos localmente sin contrato/proveedor
externo, porque generarian acoplamiento prematuro.

1. Identity global.
   - Proveedor corporativo pendiente.
   - Debe definir usuario, roles, tenant, claims y autenticacion.

2. Tenant corporativo multitenant real.
   - Depende de Identity/global tenant model.
   - El modulo ya tiene contexto desacoplado para adaptarlo.

3. Maestros globales de clientes/proveedores.
   - Pendiente contrato externo.
   - El modulo conserva contexto denormalizado y referencias externas.

4. DMS/storage documental productivo.
   - Pendiente proveedor o politica documental.
   - El storage actual es local y reemplazable por frontera.

5. Plantilla legal corporativa de PDF.
   - Requiere marca, disclaimers, firmas y politica documental oficial.

6. Firma/aprobacion documental corporativa.
   - P4 ya cubre aprobacion/rechazo interna.
   - Firma legal requiere proveedor o regla corporativa.

7. App shell global.
   - El modulo ya expone `GET /api/v1/integrations/rca/capabilities`.
   - Falta consumidor/app global real.

8. Dashboard cross-module.
   - El modulo ya expone `GET /api/v1/integrations/rca/dashboard/summary`.
   - Falta consumidor global que combine RCA con Gantt/OEE/TPM/Andon/etc.

9. Event bus/broker corporativo.
   - P2 ya deja outbox, webhooks y contratos broker-ready.
   - La conexion a broker real depende de tecnologia y convenciones externas.

10. Adapters directos de Gantt, SCADA, Gateway, OEE, TPM o Andon.
    - Deben vivir en sus modulos o adapters propios.
    - Este repo debe seguir integrando por APIs/eventos/snapshots.

11. Politica IA por tenant.
    - Requiere Identity/tenant corporativo real.
    - P3 ya soporta modo Stub/Http/fallback y aprobacion humana.

12. Verificacion adicional para actores externos.
    - Requiere Identity, proveedor de OTP o politica corporativa.

13. Antivirus/DLP documental.
    - Requiere proveedor y politica productiva.

## Pendientes de Validacion Operativa

1. Smoke manual de webhooks con receiver real.
   - Pendiente hasta tener consumidor externo.

2. Validacion visual completa con app + DB.
   - Recomendable por corte UI grande, no por cada micro-ajuste.

3. Prueba de DMS productivo.
   - Pendiente hasta definir proveedor.

4. Prueba de Identity global.
   - Pendiente hasta definir proveedor y claims.

5. Prueba de app shell/dashboard externo.
   - Pendiente hasta tener consumidor real.

## Pendientes de Limpieza del Workspace

El workspace tiene cambios no relacionados que no forman parte de los ultimos
cortes P4.4/P4.5:

- `docs/LOCAL_OPERATIONS.md`
- `src/IshikawaRca.Web/appsettings.Development.json`
- `docs/CODEX_COORDINATOR_START.md`
- `docs/CODEX_TOPIC_CHAT_START.md`
- `docs/LOGICA_DE_FUNCIONAMIENTO.md`
- `docs/chats/COORDINACION.md`
- `docs/chats/DEVOPS.md`
- `docs/chats/DOCS.md`
- `docs/chats/LOGICA_DE_FUNCIONAMIENTO.md`
- `docs/chats/ROADMAP.md`
- `docs/chats/_TEMPLATE.md`
- `docs/superpowers/plans/2026-06-04-rca-fuga-resolution.md`

Antes de un push o PR conviene decidir si esos archivos se incorporan,
se separan en otro commit o se dejan como trabajo local.

## Recomendacion de Proximo Corte

Si se quiere seguir trabajando sin depender de plataforma externa, el mejor
proximo corte es QA/operacion:

1. Ordenar suite formal de tests.
2. Agregar tests MVC/UI criticos.
3. Crear pipeline CI/CD con timeouts.
4. Validar app + DB con smoke acotado.

Si se quiere avanzar producto, el siguiente corte deberia empezar con una
decision explicita de negocio:

- SLA visual.
- Reapertura gobernada.
- Adjuntos de intake externo.
- Comentarios colaborativos.
- Vistas por rol.

## Regla de Trabajo

No implementar pendientes bloqueados por plataforma hasta tener contrato,
proveedor o consumidor externo concreto. El modulo debe mantenerse standalone y
seguir integrando por APIs, eventos, snapshots, outbox, webhooks o contratos
versionados.
