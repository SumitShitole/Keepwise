# ADR-015 Duplicate detection

- **Context:** The same order appears as email, SMS, and PDF.
- **Decision:** Fingerprint `user+vendor+(order|invoice)` else `user+vendor+amount+date+product tokens`. Cluster candidates; confirm once. Idempotent ingest via content SHA-256.
