# apps/demo-login

Demo consumer application contract for validating login through IdP + BFF.

## Demo objective

Show the first-step authentication journey end-to-end:

1. User opens demo app UI
2. UI calls `/bff/user`
3. If unauthenticated, UI redirects to `/bff/login`
4. BFF starts OIDC authorization code + PKCE with IdP
5. BFF stores tokens server-side after callback
6. Demo UI becomes authenticated using only opaque session cookie

## Minimal UI routes

- `/login` -> shows "Sign in" button that points to `/bff/login`
- `/dashboard` -> authenticated landing page

## Minimal BFF routes

- `GET /bff/login`
- `GET /bff/user`
- `GET /bff/logout`
- `GET /signin-oidc` (OIDC callback)
- `POST /backchannel-logout`

## Success criteria for the demo

- Browser storage contains no access/refresh token.
- Authentication state is driven by BFF session cookie only.
- Second configured app can sign in silently (SSO) after initial login.
