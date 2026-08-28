# Document processing

Uploads stay private (`IFileStorage`, signed/local paths, never public ACLs). Allowed: PDF, JPEG, PNG, WebP. Max 10 MB. SHA-256 is stored for idempotency.

Selectable PDF text is extracted without OCR. Images and scanned PDFs need `IOcrProvider` (Google Document AI when configured). Failed OCR leaves the candidate in `NeedsOcr` / `Failed` rather than inventing fields.

The same PurchaseCandidate pipeline is used for receipts, invoices, and pasted order text.
