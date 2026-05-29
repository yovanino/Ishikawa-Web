# Contexto Maestro

Este modulo nace dentro de una vision mayor: una AI-Native Industrial Operations Platform compuesta por modulos independientes e integrables.

La plataforma global futura puede incluir:

- Industrial Communication Gateway.
- Interactive Gantt.
- OEE Live.
- TPM.
- Andon.
- Ishikawa RCA.
- Heijunka.
- Yamazumi.
- Value Stream Mapping.
- Digital Kamishibai.
- Pareto Explorer.
- AI Gateway.
- Rules Engine.
- Digital Twin.
- Timeline operacional unificado.
- Supplier and customer external intake.

## Decision Principal

Este repositorio corresponde exclusivamente al modulo Ishikawa RCA.

Los demas modulos tendran carpetas y repositorios propios. La integracion entre modulos se realizara mediante APIs, eventos y contratos compartidos, no mediante referencias directas tempranas.

## Rol de Ishikawa RCA

Ishikawa RCA es el modulo de analisis de causa raiz operacional. Debe permitir analizar problemas de produccion, mantenimiento, calidad o seguridad usando una experiencia visual, colaborativa y trazable.

Puede ser invocado por una tarea Gantt, una alarma SCADA, un Andon, una falla TPM, una perdida OEE o una carga manual.

Tambien puede recibir contexto de reclamos internos, clientes o proveedores. En el MVP se guarda como datos propios del RCA; en la plataforma global futura debe integrarse con maestros de areas, clientes, proveedores e identidad externa.

## Regla de Integracion

El modulo debe guardar referencias externas como datos de integracion:

- `SourceSystem`
- `ExternalTaskId`
- `ExternalEventId`
- `ExternalWorkOrderId`
- `TaskSnapshotJson`
- `ContextSnapshotJson`

No debe depender de tablas internas de otros modulos durante el MVP.

## Regla para Actores Externos

Clientes y proveedores no deben acceder al modulo completo. Deben completar informacion mediante links externos seguros, con alcance limitado, expiracion, auditoria y aprobacion humana antes de incorporar su respuesta al RCA oficial.

