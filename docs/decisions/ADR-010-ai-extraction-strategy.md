# ADR-010 AI extraction strategy

- **Context:** Unstructured receipts vs structured order emails.
- **Decision:** Rules and PDF text first. LLM only when enabled by the user and required fields are missing. Strict JSON schema. Untrusted content is DATA not instructions.
- **Trade-off:** Weaker extraction on handwritten scans until OCR/LLM keys exist.
