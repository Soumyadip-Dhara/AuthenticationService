# DEMO-LOGIN

This document defines a concrete demo path for "application login" using the architecture in `AUTH-FLOWS.md`.

## Scope (first step)

- Demonstrate login from a consumer app through BFF.
- Demonstrate SSO readiness by using shared IdP session semantics.
- Keep integration configuration-driven so additional apps can be onboarded without redesign.

## Demo actors

- `Auth.IdP` - centralized identity provider
- `DemoLogin.Bff` - confidential client + app session owner
- `DemoLogin.UI` - browser-facing frontend
- `DemoLogin.Api` - protected API behind BFF

## Demo login flow

1. Open demo UI (for example `https://demo.example.com`).
2. UI checks auth state via `GET /bff/user`.
3. If 401, UI sends user to `GET /bff/login`.
4. BFF challenges OIDC and redirects to IdP `/connect/authorize`.
5. User authenticates at IdP (or is silently authenticated if SSO session exists).
6. IdP redirects back to BFF callback (`/signin-oidc`) with authorization code.
7. BFF exchanges code at `/connect/token`, stores tokens server-side, and sets opaque session cookie.
8. UI calls `/bff/user` again and shows authenticated dashboard.

## Demo logout flow

1. User selects logout from demo UI.
2. UI calls `GET /bff/logout`.
3. BFF clears local app session and redirects to IdP logout.
4. IdP clears SSO session and issues back-channel logout to registered BFFs.

## Demo verification checklist

- [ ] Login works from unauthenticated state.
- [ ] Session is maintained via opaque cookie only.
- [ ] Browser local/session storage does not contain OAuth tokens.
- [ ] `/bff/logout` clears app session.
- [ ] Back-channel logout endpoint exists and is wired in app configuration.
