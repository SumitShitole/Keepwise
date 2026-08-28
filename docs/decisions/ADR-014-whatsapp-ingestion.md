# ADR-014 WhatsApp ingestion

- **Context:** Vendors send invoices on WhatsApp.
- **Decision:** Never read the personal WhatsApp inbox. Allow share sheet, screenshot, PDF, pasted text (`WhatsAppShare`). Business Cloud API only if Keepwise is the business recipient (`WhatsAppBusiness`).
