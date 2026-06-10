# Operacion Local del Modulo

Esta guia deja el modulo Ishikawa RCA listo para levantar, validar y conectar con otros modulos sin depender de la plataforma global.

## Requisitos

- .NET SDK compatible con `global.json`.
- MySQL 8.x accesible desde la maquina local o servidor dedicado.
- Git instalado.
- Credenciales MySQL fuera del repo.

## Configuracion Local

Crear `src/IshikawaRca.Web/appsettings.Local.json` o usar variable de entorno. Este archivo no se versiona.

```json
{
  "ConnectionStrings": {
    "IshikawaRca": "Server=localhost;Port=3306;Database=ishikawa_rca;User=<user>;Password=<password>;TreatTinyAsBoolean=true;SslMode=None;AllowPublicKeyRetrieval=True;Connection Timeout=5;Default Command Timeout=15;"
  },
  "AiGateway": {
    "Mode": "Stub",
    "BaseUrl": "",
    "TimeoutSeconds": 30
  },
  "EvidenceStorage": {
    "RootPath": "App_Data"
  }
}
```

`EvidenceStorage:RootPath` define el repositorio fisico de adjuntos de evidencia. Puede ser relativo al proyecto Web, como `App_Data`, o absoluto, como `D:\IndustrialOps\EvidenceRepository`. En produccion tambien puede configurarse con la variable de entorno `EvidenceStorage__RootPath`.

Alternativa por terminal para levantar la app o ejecutar smokes:

```powershell
$env:ConnectionStrings__IshikawaRca="Server=localhost;Port=3306;Database=ishikawa_rca;User=<user>;Password=<password>;TreatTinyAsBoolean=true;SslMode=None;AllowPublicKeyRetrieval=True;Connection Timeout=5;Default Command Timeout=15;"
```

Para comandos EF design-time tambien se puede usar:

```powershell
$env:ISHIKAWA_RCA_CONNECTION=$env:ConnectionStrings__IshikawaRca
```

## Base de Datos

Aplicar migraciones:

```powershell
dotnet ef database update --project src\IshikawaRca.Infrastructure\IshikawaRca.Infrastructure.csproj --startup-project src\IshikawaRca.Web\IshikawaRca.Web.csproj --context RcaDbContext
```

Si falla por credenciales, validar usuario, password, permisos sobre `ishikawa_rca` y conectividad a `localhost:3306`.

Si Visual Studio corta una request mientras EF esta abriendo MySQL, puede verse `OperationCanceledException` en el depurador. Primero confirmar que MySQL este activo, que `appsettings.Local.json` tenga `AllowPublicKeyRetrieval=True`, y que la validacion completa pase con timeouts controlados.

## Compilacion

Validar primero que haya un SDK compatible con `global.json` registrado. El
preflight acepta versiones patch superiores dentro del mismo feature band, por
ejemplo `10.0.301` para `10.0.300`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-dotnet-sdk.ps1
```

```powershell
dotnet build IshikawaRca.sln /m:1 --no-restore
```

Usar `/m:1` porque en esta instalacion local se observo un fallo silencioso con MSBuild paralelo.

## Ejecucion

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-web.ps1 -BaseUrl http://localhost:5025 -StartupTimeoutSeconds 20
```

El script fuerza `ASPNETCORE_ENVIRONMENT=Development` para validacion local y
guarda logs de arranque en `artifacts/ishikawa-web.stdout.log` y
`artifacts/ishikawa-web.stderr.log` si el proceso falla antes de abrir el
puerto.

UI:

```text
http://localhost:5025
http://localhost:5025/Rca
```

## Validacion Rapida

Con la app corriendo y la DB migrada:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1 -BaseUrl http://localhost:5025 -RequestTimeoutSeconds 10
```

El script crea un incidente demo, agrega causa raiz y subcausa, registra
evidencia validada con adjunto, agrega y completa accion correctiva y accion
preventiva de recurrencia, escala a 8D, cierra el RCA, valida wizard/snapshot,
consulta eventos de integracion y valida los endpoints de IA en modo stub.

Contrato de errores de autorizacion API:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-api-auth-errors.ps1 -BaseUrl http://localhost:5025 -RequestTimeoutSeconds 10
```

El script valida que operaciones API protegidas devuelvan `ApiResult` con
HTTP 403 `FORBIDDEN` para rol insuficiente y HTTP 401
`AUTHENTICATION_REQUIRED` para contexto de autenticacion invalido.

Hechos externos por API:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-external-facts.ps1 -BaseUrl http://localhost:5025 -RequestTimeoutSeconds 10
```

El script crea un RCA minimo, registra un hecho externo con correlacion
`externalSourceSystem` + `externalEventId`, valida idempotencia por reintento,
lista facts y verifica rechazo `EXTERNAL_FACT_CORRELATION_INCOMPLETE`.

Validacion local completa:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-local-validation.ps1 -Build -BaseUrl http://localhost:5025 -StartupTimeoutSeconds 20 -RequestTimeoutSeconds 10 -ShutdownTimeoutSeconds 10
```

Este comando normaliza el `Path` de la sesion, valida el SDK cuando se usa
`-Build`, compila, levanta la app, espera el puerto con timeout, ejecuta el
smoke test funcional, valida errores API 401/403, facts externos e idempotencia
y detiene el proceso aunque el test falle. Omitir `-Build` solo cuando se
quiera probar un DLL ya compilado.

## Timeouts Operativos

- Startup de app local: 20 segundos.
- Requests del smoke test: 10 segundos por request.
- Shutdown de app local: 10 segundos.
- Comandos de build desde Codex: 120 segundos como maximo.

Evitar `dotnet run` interactivo para pruebas automatizadas. Usar los scripts de `scripts/` para no dejar procesos colgados.

## Cierre de Paso

Antes de sincronizar:

```powershell
dotnet build IshikawaRca.sln /m:1 --no-restore
& 'C:\Program Files\Git\cmd\git.exe' status --short --branch
```

Luego:

```powershell
& 'C:\Program Files\Git\cmd\git.exe' add .
& 'C:\Program Files\Git\cmd\git.exe' commit -m "<mensaje>"
& 'C:\Program Files\Git\cmd\git.exe' push
```
