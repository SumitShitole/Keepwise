# ADR-013 SMS ingestion

- **Context:** Indian purchase SMS is common.
- **Decision:** Do not request `READ_SMS`/`RECEIVE_SMS` (Play default-handler policy). Users paste or share message text, or upload a screenshot. No inbox polling.
