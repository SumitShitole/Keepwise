# ADR-001 Frontend architecture

- **Context:** Need web + Android with React, practical reuse, good UX.
- **Options:** React Native Web; Expo-only; Next.js + Expo + shared TS.
- **Decision:** Next.js web, Expo Android, `@keepwise/shared` for types/client.
- **Reason:** Dashboards and documents need a real web UX; RN Web would compromise both.
- **Trade-off:** Two UI codebases. Shared math stays on the API.
