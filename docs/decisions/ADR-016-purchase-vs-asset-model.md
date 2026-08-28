# ADR-016 Purchase vs asset model

- **Context:** Replacement, sale, and multiple documents need a transaction vs owned-thing split.
- **Decision:** Keep `Item` as the asset (existing APIs). Add `Purchase` 1:1 for transaction fields. Do not rename `/v1/items` in this phase.
