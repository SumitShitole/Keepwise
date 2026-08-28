# ADR-007 File storage

- **Context:** Private invoices and warranty cards.
- **Options:** Public disk, S3-only, GCS, Azure Blob.
- **Decision:** `IFileStorage` with local disk for development; production target Google Cloud Storage in Mumbai with signed URLs.
- **Reason:** Aligns with Firebase/GCP; never public ACLs; 10 MB; pdf/jpeg/png/webp.
- **Trade-off:** Malware scanning is a later hardening step.
