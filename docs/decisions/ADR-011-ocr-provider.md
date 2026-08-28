# ADR-011 OCR provider

- **Context:** Images and scanned PDFs need text.
- **Options:** Tesseract only; Azure Form Recognizer; Google Document AI.
- **Decision:** `IOcrProvider` abstraction. Production target Document AI (GCP). Dev/no-key: no-op leaving `NeedsOcr`.
- **Trade-off:** Images without OCR keys will not fully extract until configured.
