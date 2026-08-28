# Android screens (parity with web destinations)

Status: shipped.

## Goal

Let a signed-in user open every Keepwise destination on Android without a second UI library: Dashboard, Inbox, Inbox review, Items, Add item, Item detail, Settings. Sign-in remains the unauthenticated screen.

## Approach

- Local screen state in `apps/mobile` (tabs + stack). Do not add React Navigation or Expo Router.
- Call the API via `@keepwise/shared` (`createApiClient`). Do not share web screens.
- Metro watches the monorepo so the workspace package resolves.
- Receipt file upload stays web-only (needs a document picker). Inbox supports paste/extract text and candidate confirm/ignore.
