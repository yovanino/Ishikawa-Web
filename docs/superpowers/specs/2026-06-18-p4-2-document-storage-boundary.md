# P4.2 Document Storage Boundary Spec

Date: 2026-06-18.

## Goal

Keep RCA closure PDFs and future governed documents behind a storage boundary
that works standalone today and can be replaced by a corporate DMS adapter
later.

## Current State

P4.1 added local `IClosureDocumentStorage` in the Web layer for generated
closure PDFs and `RcaClosureDocument` metadata in the backend.

## Scope

- Preserve local filesystem storage as the default standalone mode.
- Keep storage metadata portable: provider, key, content type, size and SHA-256.
- Require controlled resolution/download through application endpoints.
- Document the future DMS adapter contract without adding a vendor dependency.

## Out Of Scope

- Direct dependency on SharePoint, S3, Azure Blob, Documentum or another DMS.
- Migrating existing evidence attachments to a new provider.
- Retention/legal-hold policies not defined by the corporate platform.

## Future Adapter Contract

A productive adapter must provide:

- `SaveAsync` with content bytes/stream, file metadata and tenant/RCA scope.
- `Resolve` or signed/download URL generation with authorization checks.
- SHA-256 verification or pass-through hash validation.
- Explicit failure codes for unavailable provider, rejected content and missing
  document.
- Configurable timeout so no publication/download path can hang indefinitely.

## Acceptance

- Local standalone mode remains default.
- No external SDK is referenced from this repository until the platform chooses
  a provider.
- The public API continues returning document metadata independent of provider.
