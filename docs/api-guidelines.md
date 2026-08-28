# API guidelines

Base URL `/v1`. JSON. Bearer auth except `/health` and `/v1/auth/dev-login` (dev only).

| Method | Path | Purpose |
| --- | --- | --- |
| POST | /v1/auth/dev-login | Passwordless dev session |
| GET | /v1/users/me | Profile |
| PUT | /v1/users/me | Profile + channel prefs |
| GET | /v1/catalog/categories | Categories |
| GET | /v1/dashboard | Attention + counts |
| POST | /v1/ingestion/text | Import shared order text |
| POST | /v1/ingestion/documents | Import receipt/PDF |
| GET | /v1/purchase-candidates | Inbox |
| GET/PUT | /v1/purchase-candidates/{id} | Candidate detail / edit |
| POST | /v1/purchase-candidates/{id}/confirm | Create Purchase + Item |
| POST | /v1/purchase-candidates/{id}/ignore | Dismiss candidate |
| GET/PUT | /v1/users/me/ingestion-settings | Detection toggles |
| GET | /v1/privacy | Privacy summary |
| GET/POST | /v1/items | Search / create |
| GET/PUT/DELETE | /v1/items/{id} | Item CRUD |
| POST | /v1/items/{id}/coverages | Add warranty/maintenance/renewal/return |
| PUT | /v1/coverages/{id} | Update coverage |
| POST | /v1/coverages/{id}/extend | Extend warranty |
| POST | /v1/coverages/{id}/complete | Complete maintenance |
| POST | /v1/coverages/{id}/skip | Skip maintenance |
| POST | /v1/coverages/{id}/reschedule | Reschedule maintenance |
| POST | /v1/items/{id}/attachments | Upload |
| GET/DELETE | /v1/attachments/{id} | Download / delete |
| GET | /v1/notifications | Recent reminder occurrences |

Errors: `{ "error": { "code": "...", "message": "..." } }`. Pagination: `page`, `pageSize`.
