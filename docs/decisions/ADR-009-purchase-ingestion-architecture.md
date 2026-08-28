# ADR-009 Purchase ingestion architecture

- **Context:** Manual item entry is too slow; users have receipts, PDFs, and order text.
- **Options:** Silent auto-create; per-source microservices; in-process pipeline + Hangfire + confirm inbox.
- **Decision:** Modular-monolith ingestion module. All sources produce `PurchaseCandidate`. Confirm is always required. Hangfire processes jobs asynchronously.
- **Trade-off:** One extra review step vs silent errors.
