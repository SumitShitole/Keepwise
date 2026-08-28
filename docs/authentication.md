# Authentication

Passwordless only. No password hashes.

**Development:** `POST /v1/auth/dev-login` with `{ email, displayName }` provisions a user (`firebase_uid` = `dev:{email}`) and returns an HS256 JWT. Enabled when `Auth:AllowDevLogin` is true.

**Production:** Firebase Authentication (email magic link + Google). The API validates Firebase ID tokens (`iss`/`aud` = project id). First request provisions `users` by Firebase UID.

Clients store the access token and send `Authorization: Bearer`. Logout is client-side token discard (Firebase sign-out in production).

Phone OTP, account linking, and Firebase email/phone change flows are documented for a later slice. Account deletion and DPDP residency of Firebase identity require legal review.
