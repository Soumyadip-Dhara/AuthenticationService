# AUTH-FLOWS

This document defines the first-step reference architecture for a centralized authentication platform that supports:

1. OpenID Connect authorization code + PKCE
2. BFF-based session handling (no access/refresh tokens in browser storage)
3. SSO across multiple applications
4. Back-channel logout fan-out
5. Configuration-driven onboarding for new applications

---

## Core principles

- **IdP owns identity and tokens**.
- **BFF owns browser session cookie** and token storage server-side.
- **Frontend never stores OAuth tokens** in local/session storage.
- **Each app integrates by config**, not by rewriting auth logic.

---

## Flow summary

### 1) First login (no SSO session)

1. Frontend calls `/bff/user` and gets `401`.
2. Frontend redirects user to `/bff/login`.
3. BFF initiates OIDC authorization code flow with PKCE.
4. IdP prompts login, validates credentials, creates IdP SSO cookie.
5. IdP returns authorization code to BFF callback.
6. BFF exchanges code for tokens and stores tokens server-side.
7. BFF issues opaque app session cookie to browser.

### 2) Second app login (SSO)

1. User opens another app and starts `/bff/login`.
2. Browser already has IdP SSO cookie.
3. IdP skips login UI and returns code immediately.
4. App-specific BFF creates app session cookie.

### 3) API access through BFF

1. Browser calls BFF with opaque session cookie.
2. BFF reads server-side access token and forwards API request with an authorization header.
3. Resource API validates token (introspection or JWT validation strategy).

### 4) Single logout

1. User triggers `/bff/logout`.
2. BFF clears app session and calls IdP logout.
3. IdP clears SSO session and dispatches back-channel logout to registered BFFs.
4. Other BFF sessions for same SSO `sid` are revoked.

---

## Configuration-driven app onboarding

To onboard a new application, register it in configuration with:

- `client_id`
- redirect URIs
- post logout redirect URIs
- API audience / scopes
- BFF cookie naming
- back-channel logout URI

Reference: `../configs/applications.example.yaml`.

Demo walkthrough: `./DEMO-LOGIN.md`.
