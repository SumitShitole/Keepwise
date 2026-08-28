# Development guidelines

- C# namespaces match projects: `Keepwise.Domain`, `.Application`, `.Infrastructure`, `.Api`.
- TypeScript apps: `@keepwise/web`, `@keepwise/mobile`, `@keepwise/shared`.
- Configuration: `appsettings.json` + environment variables. Never commit real secrets. See `appsettings.example.json` and `.env.example`.
- Git: `main` plus short-lived `feature/*` branches. Conventional Commits. PRs for review when collaborating.
- Local API: `http://127.0.0.1:43124`. Web: `http://127.0.0.1:43123`.
- Start both with `scripts/dev.ps1` (Windows) or `scripts/dev.sh` (macOS/Linux). See the README Run section.
- Dependency injection: register in `DependencyInjection` classes. No service locator except Hangfire activation.
- Errors: global middleware maps exceptions to `{ error: { code, message } }`.
- Logging: Serilog console JSON-friendly text. No tokens or passwords.
