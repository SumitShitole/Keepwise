# ADR-006 Notification providers

- **Context:** Push, email, SMS, WhatsApp with India availability.
- **Options:** Tight coupling to Twilio/Firebase; abstraction + cheap India providers.
- **Decision:** `INotificationSender` per channel. MVP: FCM (later) + Brevo email; logging in development. SMS MSG91 and WhatsApp Gupshup/Meta as stubs.
- **Reason:** SMS/WhatsApp DLT cost must not block MVP. Providers stay replaceable.
- **Trade-off:** Dev does not send real email/push until credentials are configured.
