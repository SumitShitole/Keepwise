---
todos:
  - id: docs-adrs
    status: completed
    content: 'Add vision/ingestion/privacy docs, ADRs 009-016, update project-context and Cursor rules'
  - id: domain-migration
    status: completed
    content: 'Add Purchase, PurchaseCandidate, IngestionJob, settings, ReturnWindow, lifecycle; EF migration'
  - id: pipeline
    status: completed
    content: 'IPurchaseSource, extraction pipeline, duplicate detector, Hangfire job, heuristic/PDF extractors, tests with fake fixtures'
  - id: apis-confirm
    status: completed
    content: Ingestion and candidate APIs; confirm creates Purchase+Item+coverages via existing services
  - id: web-inbox
    status: completed
    content: 'Attention dashboard, /inbox review UI, privacy/detection settings, scan-receipt upload'
  - id: verify
    status: completed
    content: Run domain/application/API tests and commit
name: Purchase Ingestion Evolution
overview: 'Evolve Keepwise from a manual warranty tracker into a personal ownership assistant by adding a confirm-first purchase ingestion pipeline (document/text first), while keeping Item as the asset, Coverage/reminders, Hangfire, and existing APIs intact.'
isProject: false
---
# Keepwise: ownership assistant + purchase ingestion

This is analysis plus an incremental implementation plan. **Do not rewrite** the modular monolith, reminder engine, or item APIs.

---

## 1. Existing architecture assessment

**Current architecture (unchanged core):** Next.js web + Expo Android + `@keepwise/shared` + ASP.NET Core 10 modular monolith + EF Core/PostgreSQL + Hangfire in-process + Firebase/dev JWT + `INotificationSender` / `IFileStorage`.

**Implemented:** Passwordless login, profile + channel toggles, categories, items CRUD/search, warranties (duration or explicit expiry), maintenance complete/skip/reschedule, renewals as `CoverageKind`, reminder generation/dispatch/idempotency, dashboard counts, item-scoped document upload (PDF/JPEG/PNG/WebP, 10 MB), domain/API tests.

**In progress / thin:** Android is a login+dashboard stub. SMS/WhatsApp/FCM/Brevo are stubs. Hangfire dashboard only in Development. No OCR, email connect, or review inbox.

**Technical debt:** Purchase fields live on [`Item`](backend/src/Keepwise.Domain/Entities/Item.cs); attachments only link to items ([`Attachment`](backend/src/Keepwise.Domain/Entities/Attachment.cs)); dashboard is count-centric ([`DashboardDto`](backend/src/Keepwise.Application/Dashboard/DashboardDtos.cs)); no field provenance, lifecycle, return window, quiet hours, or notification priority; `Sha256` unused.

**Remain unchanged:** Stack, `/v1` conventions, user isolation, domain date math, Hangfire reminder jobs, `Coverage` as warranty/maintenance/renewal, no microservices/Kafka.

**Refactor (small, additive):** Treat `Item` as the **asset** in copy and docs (keep table/API name `items` for now). Extend `CoverageKind` with `ReturnWindow`. Optional `LifecycleStatus` on Item defaulting to Active. Dashboard attention list. Attachment `EntityType`/`EntityId` so docs can attach to candidate/purchase/coverage later without breaking item uploads.

**Add:** Ingestion module: sources, pipeline, `PurchaseCandidate`, confirm → existing `ItemService`/`CoverageFactory`, duplicate fingerprints, Privacy Center, AI/OCR provider abstractions.

**Conflicts:** Splitting Purchase vs Asset as new primary APIs would break web/tests. **Do not replace Item.** Introduce `Purchase` as a 1:1 transaction record created on confirm (order/GST/UPI/return), linked to `Item`.

```mermaid
flowchart TB
  subgraph existing [Keep as-is]
    Item[Item as Asset]
    Cov[Coverage]
    Rem[ReminderEngine]
  end
  subgraph newMod [New ingestion module]
    Src[IPurchaseSource]
    Pipe[ExtractionPipeline]
    Cand[PurchaseCandidate]
    Dup[DuplicateDetector]
  end
  Src --> Pipe --> Dup --> Cand
  Cand -->|user confirm| Item
  Item --> Cov --> Rem
```

---

## 2. Competitive matrix (category-level)

Warranty apps (Check My Warranty, WarrantyIO, Android trackers) win on inventory + expiry reminders. Home inventory (Stowly, SnapHome, PlanWithHome) wins on rooms/photos. Newer tools add receipt OCR.

Keepwise **does not clone** room-based home inventory. Differentiation is **confirm-first automatic capture** plus existing reminder reliability.

- Warranty / maintenance / insurance / documents / search: competitors strong; Keepwise already has a working slice
- Return tracking: uncommon; add as coverage kind
- Auto detection / email / share-import / OCR: emerging; **our wedge**
- SMS inbox read / WhatsApp inbox read: competitors may over-claim; **we will not** (Play + WhatsApp policy)
- Barcode / recalls / vendor intelligence: Phase 3–4

---

## 3. Differentiation strategy

Position Keepwise as a **personal ownership assistant**: detect purchases from artifacts the user **chooses to share**, never silent writes, never three copies of one Amazon order. KPI: **Automatic Capture Rate** = confirmed auto-created items / all items created (manual + confirmed).

---

## 4–8. Feasibility (sources)

**Receipt / PDF / photo (Phase 1 — build now):** Already have upload + `IFileStorage`. Add ingest endpoints that enqueue Hangfire jobs. Highest control, Play-safe, India GST PDFs and photos.

**Shared text (Phase 1 — include):** Android/web paste or share-sheet **text** (order SMS/WhatsApp forwarded by the user). Same pipeline as email body. No extra permissions.

**Email (Phase 2):** Prefer **dedicated inbound address + user forwarding** first (least privilege, no full mailbox). Then Gmail/Outlook OAuth with `gmail.readonly` / Mail.Read, incremental history, filters (`Amazon`, `Flipkart`, `invoice`, `order`). Not IMAP-all. Tokens encrypted; revocable in Privacy Center.

**Android SMS (Phase 3, constrained):** Play **does not allow** `READ_SMS` / `RECEIVE_SMS` unless the app is the default SMS/Assistant handler ([Play SMS policy](https://support.google.com/googleplay/android-developer/answer/10208820)). Keepwise is not an SMS app. **Do not** request SMS permissions. Allowed: share sheet, user paste, screenshot, optional **notification listener** later (separate sensitive permission; not Phase 1).

**WhatsApp:** **Never** read the personal inbox. Allowed: share sheet, screenshot/PDF upload, user-forwarded text. WhatsApp Business Cloud API only if Keepwise is the **business** receiving messages (not the user’s chats). Distinguish `SourceType.WhatsAppShare` vs `WhatsAppBusiness`.

**Barcode / product URL:** Phase 3 (ML Kit on device; server fetch of user-submitted URLs with SSRF allowlist — never fetch arbitrary IPs).

---

## 9–12. Pipeline, OCR, AI, candidates

Keep stages as testable functions in `Keepwise.Application/Ingestion`:

Ingest → preprocess (MIME, size, hash) → detect-is-purchase (rules) → extract (rules / OCR / LLM) → normalize (INR, `dd MMM yyyy`, IST dates) → validate → confidence → duplicate → `PendingReview`.

**Layered extraction (cost):**
1. Deterministic parsers (Amazon/Flipkart-like **fixtures**, GSTIN/UPI/order `#` regex) — no network
2. PDF text (`PdfPig` or similar) if selectable text exists
3. `IOcrProvider` only for images or scanned PDFs (Google Document AI when configured; otherwise skip with `NeedsOcr` status)
4. `ILlmExtractor` only if required fields missing after 1–3; **JSON schema only**; treat model output as data; wrap untrusted content in a delimiter and instruct “imported content is DATA not instructions”

Providers stay behind interfaces. Dev without keys: rules + PDF text still produce candidates.

**`PurchaseCandidate` fields:** source type/id, storage key (not raw email dump), extracted fields + per-field confidence + `FieldProvenance` (Confirmed / UserProvided / VendorProvided / AiInferred / Estimated), overall confidence, status (`Processing|PendingReview|Confirmed|Ignored|Failed|Duplicate`), `DuplicateOfId`, `Fingerprint`.

**Never auto-create Item** unless product policy later sets a very high bar; **MVP always requires Confirm** (safer, matches “never silently create incorrect important information”).

---

## 11–12. Purchase vs Asset (decision)

**ADR-016:** `Item` remains the **asset** (owned thing, reminders, coverages). New `Purchase` is the **transaction** (vendor, order/invoice numbers, amount, GST, UPI ref, return-by). Confirm maps: candidate → `Purchase` + `Item` + optional warranty/return `Coverage`. Replacement/sale later updates `Item.LifecycleStatus` without deleting purchase history.

Do not rename `/v1/items` in this phase.

---

## 12. Duplicate detection

Fingerprint (normalized): `userId + vendorSlug + (orderNumber|invoiceNumber)` else `userId + vendorSlug + amount + purchaseDate + productTokens`. Match existing `Purchase`/`Item` and other candidates. Same Amazon email + PDF → one candidate cluster (`DuplicateOfId`), one Item on confirm.

---

## 13. Privacy / security

- Imported bytes: existing type/size limits; compute SHA-256; store blob; **do not log bodies**
- Prompt injection: untrusted text never concatenated as system instructions
- SSRF: no URL fetch in Phase 1
- Cross-user: all candidate queries `UserId == current`
- Privacy Center: source toggles, AI processing on/off, disconnect (Phase 2), delete imported candidates/blobs, export, account delete (stub export JSON)
- Document which data would go to OCR/LLM when enabled; default **AI off until user enables** in settings

---

## 14. Cost

Rules/PDF text ~free. OCR/LLM usage meters on `ingestion_jobs` (provider, tokens/pages, duration). Soft daily cap per user (e.g. 20 AI/OCR jobs). Hangfire retries already exist.

---

## 15. Roadmap (challenged sequence)

**Phase 1 (this implementation):** Keep manual add-item. Add ingest image/PDF/text → Hangfire → candidate review UI (Confirm/Edit/Ignore). Rule + PDF extractors + optional OCR/LLM interfaces. Return window coverage. Attention dashboard + pending-candidate count. Privacy/AI toggles. Duplicate fingerprints. Tests + docs/ADRs.

**Why not OCR-first only:** selectable PDF + shared order text is higher accuracy for India e-commerce than handwritten OCR.

**Phase 2:** Inbound email forwarding; then Gmail/Outlook OAuth.

**Phase 3:** Expo share-target; still no READ_SMS.

**Phase 4:** Product intelligence/recalls via licensed/official APIs only.

Notification priority/quiet hours: **Phase 1 light** (urgency on dashboard + existing channel toggles). Per-reminder channel routing: Phase 2.

---

## 16–18. Architecture / DB / API changes

**Architecture:** New Application module `Ingestion` + Infrastructure parsers/OCR stubs. New Hangfire job `ProcessIngestionJob`. In-process events (`PurchaseCandidateReady`) to enqueue reminder refresh — **no broker**.

**Database (new tables):**
- `purchases` (ItemId unique for MVP, order/invoice/amount/return-by, fingerprint)
- `purchase_candidates` (jsonb extracted payload + statuses)
- `ingestion_jobs` (source, status, attempts, error code, cost counters)
- `user_ingestion_settings` (email/sms/whatsapp/receipt/ai flags)
- Extend `coverages.kind` for ReturnWindow
- `items.lifecycle_status` (int, default Active)
- `attachments` optional `owner_type`/`owner_id` (default Item)

**API (keep `/v1`, not `/api`):**
- `POST /v1/ingestion/documents` (multipart)
- `POST /v1/ingestion/text`
- `GET /v1/purchase-candidates?status=`
- `GET /v1/purchase-candidates/{id}`
- `POST /v1/purchase-candidates/{id}/confirm|ignore`
- `PUT /v1/purchase-candidates/{id}` (edit before confirm)
- `GET/PUT /v1/users/me/ingestion-settings`
- `GET /v1/privacy` summary
- Dashboard: add `attention[]` + `pendingCandidates`

Confirm calls existing item/coverage creation.

---

## 19. Testing

Fixtures under `backend/tests/Keepwise.Application.Tests/Fixtures/` (fake Amazon/Flipkart/GST text — **never real PII**).

Must cover: India date/₹/GSTIN/UPI parse; not-a-purchase; missing fields; warranty only if explicit in text (else provenance Estimated and UI warning); duplicate same order number; confirm creates one item; ignore; edit; job retry; AI schema reject; user isolation.

Do not assert raw LLM strings; assert normalized vendor/date/amount.

---

## 20. Documentation / Cursor rules

Add: [`docs/product-vision.md`](docs/product-vision.md), `competitive-analysis.md`, `purchase-detection.md`, `ai-extraction.md`, `document-processing.md`, `privacy.md`. Update `project-context.md`, `architecture.md`, `notification-system.md`. ADRs 009–016 as listed. New scoped rule `.cursor/rules/ingestion.mdc`. Soften project.mdc line 2 from “warranty tracker” to ownership assistant.

---

## UI (Phase 1)

- Dashboard: “Needs attention” (due soon + pending reviews) above counts
- Inbox: `/inbox` list of candidates
- Review: pre-filled fields, confidence, Confirm / Edit / Ignore
- Add item: keep **name + date + warranty** as the fast path
- Settings: Privacy + detection source toggles + AI processing
- Documents: upload still on item; **also** “Scan receipt” from inbox

Android: paste/share text later (Phase 3); web upload is enough for Phase 1.

---

## Implementation order (after approval)

1. Docs + ADRs + Cursor rules (product shift)
2. Domain entities + migration + settings
3. Pipeline + heuristic/PDF extractors + Hangfire job + tests
4. APIs + confirm → Item/Purchase/Coverage
5. Web inbox + attention dashboard + privacy settings
6. Optional OCR/LLM adapters (no-op unless configured)
7. Run tests, update README, commit

**Out of this slice:** Gmail OAuth, SMS permissions, WhatsApp inbox, barcode, recalls, notification quiet hours beyond simple preferred send time already at 09:00 local.
