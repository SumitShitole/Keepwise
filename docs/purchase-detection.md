# Purchase detection

All sources implement `IPurchaseSource` and produce a `PurchaseCandidate`. Users must **Confirm** before an `Item` (asset) and `Purchase` (transaction) are created.

Pipeline: ingest → preprocess → detect → extract (rules, PDF text, optional OCR/LLM) → normalize → validate → confidence → duplicate fingerprint → `PendingReview`.

Phase 1 sources: document upload (image/PDF) and pasted/shared text. Email OAuth, SMS permissions, and WhatsApp inbox access are out of scope.

Fingerprints: `user + vendor + order/invoice` else `user + vendor + amount + date + product tokens`. Duplicates cluster via `DuplicateOfId`.

Imported content is DATA, never instructions. Bodies are not written to logs.
