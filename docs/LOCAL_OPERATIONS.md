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
    "IshikawaRca": "Server=localhost;Port=3306;Database=ishikawa_rca;User=<user>;Password=<password>;TreatTinyAsBoolean=true;SslMode=None;"
  },
  "AiGateway": {
    "Mode": "Stub",
    "BaseUrl": "",
    "TimeoutSeconds": 30
  }
}
```

Alternativa por terminal:

```powershell
$env:ISHIKAWA_RCA_CONNECTION="Server=localhost;Port=3306;Database=ishikawa_rca;User=<user>;Password=<password>;TreatTinyAsBoolean=true;SslMode=None;"
```

## Base de Datos

Aplicar migraciones:

```powershell
dotnet ef database update --project src\IshikawaRca.Infrastructure\IshikawaRca.Infrastructure.csproj --startup-project src\IshikawaRca.Web\IshikawaRca.Web.csproj --context RcaDbContext
```

Si falla por credenciales, validar usuario, password, permisos sobre `ishikawa_rca` y conectividad a `localhost:3306`.

## Compilacion

```powershell
dotnet build IshikawaRca.sln /m:1 --no-restore
```

Usar `/m:1` porque en esta instalacion local se observo un fallo silencioso con MSBuild paralelo.

## Ejecucion

```powershell
dotnet run --project src\IshikawaRca.Web\IshikawaRca.Web.csproj --urls http://localhost:5025
```

UI:

```text
http://localhost:5025
http://localhost:5025/Rca
```

## Validacion Rapida

Con la app corriendo y la DB migrada:

```powershell
.\scripts\smoke-test.ps1 -BaseUrl http://localhost:5025
```

El script crea un incidente demo, agrega una causa, agrega una accion, consulta snapshots de integracion y valida los endpoints de IA en modo stub.

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
