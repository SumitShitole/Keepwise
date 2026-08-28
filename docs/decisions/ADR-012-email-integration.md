# ADR-012 Email integration

- **Context:** Order mail is high-signal.
- **Decision:** Phase 2. Prefer inbound forwarding address first, then Gmail/Outlook OAuth with least privilege and incremental sync. Not Phase 1. Never download entire mailboxes.
