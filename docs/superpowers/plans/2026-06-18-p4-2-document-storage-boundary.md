# P4.2 Document Storage Boundary Plan

## Goal

Move from local-only closure document storage toward a replaceable document
storage boundary without coupling Ishikawa RCA to a corporate DMS.

## Tasks

- [ ] Extract a backend/application-level document storage interface if future
  non-MVC producers need to generate governed documents.
- [ ] Keep `ClosureDocumentStorage` as the default local implementation.
- [ ] Add provider option names for future adapters without shipping secrets.
- [ ] Add validation for configured provider, timeout and max document size.
- [ ] Add tests for provider selection once a second provider exists.
- [ ] Keep downloads routed through API/MVC endpoints, never public paths.

## Validation

Run in series:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Any command that starts a server, watcher or browser must use explicit timeouts
and verify no process remains alive.
