# Ishikawa RCA Module

Modulo independiente para analisis visual de causa raiz tipo Ishikawa, orientado a operacion industrial y preparado para integrarse con Gantt, SCADA, MRP, MES, Andon, TPM, OEE y una plataforma global futura.

Este repositorio no contiene la plataforma industrial completa. Contiene solo el modulo Ishikawa RCA, con UI, API, modelo de datos y contratos de integracion propios.

## Vision

Construir una experiencia moderna de Root Cause Analysis operacional:

- Wizard guiado para definir problema, impacto, severidad, causas y acciones.
- Canvas visual tipo espina de pescado con nodos editables.
- Categorias dinamicas: Metodo, Maquina, Material, Mano de obra, Medicion y Medio ambiente.
- Integracion con sistemas externos mediante APIs y eventos, sin acoplamiento directo.
- Preparacion para consumir un AI Gateway compartido por toda la plataforma industrial.

## Principio Modular

Cada modulo de la plataforma debe poder correr solo y tambien integrarse despues en una app global.

Para Ishikawa RCA esto significa:

- UI propia.
- API propia.
- Persistencia propia.
- Contratos de integracion documentados.
- Eventos de dominio publicados.
- Sin dependencia directa del Gantt ni del SCADA.

## Integraciones Previstas

El modulo podra recibir problemas desde:

- Interactive Gantt.
- SCADA o Industrial Communication Gateway.
- Andon.
- TPM.
- OEE.
- MRP/MES.
- Entrada manual de operador o supervisor.

Y podra devolver:

- Estado del analisis.
- Causa raiz seleccionada.
- Acciones correctivas.
- Evidencias.
- Severidad.
- Cierre o escalamiento a 8D.

## Stack Objetivo

- ASP.NET Core MVC.
- ASP.NET Core APIs.
- Entity Framework Core.
- MySQL.
- JavaScript modular.
- HTML absoluto + CSS transforms para el canvas visual.
- SignalR en fases posteriores.

## Documentacion

- [Contexto maestro](docs/MASTER_CONTEXT.md)
- [Limites del modulo](docs/MODULE_BOUNDARIES.md)
- [Contratos API y eventos](docs/API_CONTRACTS.md)
- [Integracion con IA](docs/AI_INTEGRATION.md)
- [Roadmap](docs/ROADMAP.md)

