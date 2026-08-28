# ADR-003 Authentication provider

- **Context:** Passwordless web + Android + ASP.NET Core.
- **Options:** Custom OTP only; Auth0; Entra External ID; Firebase Auth.
- **Decision:** Firebase Authentication in production; local HS256 dev login for development.
- **Reason:** Native email-link and Google on web and Android; FCM already required for push.
- **Trade-off:** Identity data may leave India (DPDP legal review). `FirebaseUid` kept on `users` to allow a future issuer swap.
