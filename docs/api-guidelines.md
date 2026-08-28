# API guidelines

Base URL `/v1`. JSON. Bearer auth except `/health` and `/v1/auth/dev-login` (dev only).

| Method | Path | Purpose |
| --- | --- | --- |
| POST | /v1/auth/dev-login | Passwordless dev session |
| GET | /v1/users/me | Profile |
| PUT | /v1/users/me | Profile + channel prefs |
| GET | /v1/catalog/categories | Categories |
| GET | /v1/dashboard | Summary |
| GET/POST | /v1/items | Search / create |
| GET/PUT/DELETE | /v1/items/{id} | Item CRUD |
| POST | /v1/items/{id}/coverages | Add warranty/maintenance/renewal |
| POST | /v1/coverages/{id}/extend | Extend warranty |
| POST | /v1/coverages/{id}/complete | Complete maintenance |
| POST | /v1/items/{id}/attachments | Upload |
| GET/DELETE | /v1/attachments/{id} | Download / delete |

Errors: `{ "error": { "code": "...", "message": "..." } }`. Pagination: `page`, `pageSize`.
