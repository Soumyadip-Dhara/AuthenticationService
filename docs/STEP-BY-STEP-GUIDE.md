# Step-by-Step Guide — SSO Login via IdP and BFF

A numbered, arrow-format walkthrough of every auth flow in this system.
Companion to [SSO-AUTH-FLOW-DIAGRAMS.md](SSO-AUTH-FLOW-DIAGRAMS.md).

---

## Flow 1 — First login (no existing SSO session)

> User opens the Demo app for the very first time. No IdP cookie exists yet.

```
Browser
  │
  ├─① GET https://demo.example.com/
  │       ↓
  │   Demo.Angular loads
  │
  ├─② GET /bff/user  ──────────────────────────────► Demo.Bff
  │                                                       │
  │   ◄─────────────────────────────── 401 Unauthorized ─┘
  │
  ├─③ Redirect → /login  (guard fires in Angular)
  │
  ├─④ GET /bff/login  ─────────────────────────────► Demo.Bff
  │                                                       │
  │        [Bff builds PKCE verifier + code_challenge]    │
  │   ◄──────── 302 → /connect/authorize?code_challenge=… ┘
  │
  ├─⑤ GET /connect/authorize  ─────────────────────► Auth.IdP
  │                                                       │
  │        [IdP: no .idp.session cookie → must login]     │
  │   ◄──────────────────── 302 → /Account/Login ─────────┘
  │
  ├─⑥ Load login form  ────────────────────────────► Auth.Angular (UI at IdP)
  │
  ├─⑦ POST /Account/LoginApi {username, password}  ► Auth.IdP
  │                                                       │
  │        [IdP: bcrypt-validate creds via UserStore]     │
  │        [Mint sid = Guid.NewGuid()]                    │
  │        [SignInAsync → Set-Cookie .idp.session]        │
  │   ◄── { returnUrl: /connect/authorize?… } ────────────┘
  │
  ├─⑧ window.location = returnUrl
  │
  ├─⑨ GET /connect/authorize  (with .idp.session) ─► Auth.IdP
  │                                                       │
  │        [Session valid → ClaimsBuilder copies sid]     │
  │        [Issue authorization code]                     │
  │   ◄──────────── 302 → /signin-oidc?code=… ────────────┘
  │
  ├─⑩ GET /signin-oidc?code=…  ───────────────────► Demo.Bff
  │                                                       │
  │        [Bff: POST /connect/token (code+PKCE+secret)]  │
  │        [Receives access + id + refresh tokens]        │
  │        [Stores tokens server-side (InMemoryTicketStore)] │
  │        [SidIndex: sid → sessionKey]                   │
  │        [Set-Cookie .demo.session=<random>]            │
  │   ◄──────────── 302 → https://demo.example.com/ ──────┘
  │
  ├─⑪ GET /bff/user  (with .demo.session) ─────────► Demo.Bff
  │   ◄──────────── 200 {sub, name, email, …} ─────────────┘
  │
  └─⑫ Dashboard renders — user is authenticated
       (No token in browser storage — only opaque cookie)
```

---

## Flow 2 — SSO silent login (second app, no password prompt)

> User already logged in via Demo app. Now opens CTS app — no password needed.

```
Browser
  │
  ├─① GET https://cts.example.com/
  │       ↓
  │   Cts.Angular → GET /bff/user → 401
  │   Angular guard → GET /bff/login
  │
  ├─② GET /bff/login  ─────────────────────────────► Cts.Bff
  │   ◄──── 302 → /connect/authorize?code_challenge=… ────┘
  │
  ├─③ GET /connect/authorize  ─────────────────────► Auth.IdP
  │       (browser automatically sends .idp.session)
  │                                                       │
  │        [IdP: AuthenticateAsync("idp-session") OK]     │
  │        [NO login form rendered]                       │
  │        [Issue code for cts-api audience]              │
  │   ◄──────────── 302 → /signin-oidc?code=… ────────────┘
  │
  ├─④ GET /signin-oidc?code=…  ───────────────────► Cts.Bff
  │        [Token exchange, store server-side]            │
  │   ◄── Set-Cookie .cts.session — 302 → CTS frontend ──┘
  │
  └─⑤ CTS dashboard renders
       User was never shown a login screen.
```

---

## Flow 3 — Authenticated API call through BFF

> User calls an API route. BFF injects server-side access token; browser never sees it.

```
Browser
  │
  ├─① GET /api/resource  (sends .demo.session cookie) ► Demo.Bff
  │                                                          │
  │        [Bff: cookie → ticket lookup → principal]         │
  │        [RequireAuthorization passes]                     │
  │        [YARP transform: strip browser cookies]           │
  │        [GetTokenAsync("access_token") from ticket]       │
  │                                                          │
  ├─② GET /api/resource  Authorization: ****** ──────► Demo.Api
  │                                                          │
  │        [Api: introspection path]                         │
  │        [SHA256(token) → cache lookup]                    │
  │                                                          │
  │        ── cache MISS ──                                  │
  ├─③ POST /connect/introspect  ──────────────────────► Auth.IdP
  │   ◄── { active, sub, aud, scope, exp } ──────────────────┘
  │        [Api: validate aud=demo-api, scope=api:demo]      │
  │        [Cache result until min(exp, MaxCacheTtl)]        │
  │        [Build ClaimsPrincipal → [Authorize] passes]      │
  │   ◄──────────────────── 200 resource data ───────────────┘
  │
  └─② GET response returned to Browser — no token exposed
```

---

## Flow 4 — Single logout with back-channel fan-out

> User logs out of Demo app. All SSO-linked apps are also logged out server-to-server.

```
Browser
  │
  ├─① GET /bff/logout  ────────────────────────────► Demo.Bff
  │        [SignOut("demo-cookie","oidc")]                    │
  │        [Remove ticket from TicketStore]                  │
  │        [Expire .demo.session cookie]                     │
  │   ◄── 302 → /connect/logout?id_token_hint=… ────────────┘
  │
  ├─② GET /connect/logout  ────────────────────────► Auth.IdP
  │        [Parse id_token_hint → extract sub + sid]
  │        [Identify all BFFs registered for this client]
  │
  │        ── Back-channel fan-out (server-to-server, NOT via browser) ──
  │
  ├─③a Auth.IdP  ──POST /backchannel-logout──────────► Cts.Bff
  │              (logout_token, RS256 signed)                │
  │              [Validate iss/aud/sig/events claim]         │
  │              [SidIndex.GetSessions(sid) → all keys]      │
  │              [TicketStore.RemoveAsync each key]          │
  │              [Clear sid from index]                      │
  │   ◄─────────────────────────── 200 OK ───────────────────┘
  │
  ├─③b Auth.IdP  ──POST /backchannel-logout──────────► Billing.Bff
  │   ◄─────────────────────────── 200 OK ───────────────────┘
  │
  ├─③c Auth.IdP  ──POST /backchannel-logout──────────► Demo.Bff (idempotent)
  │   ◄─────────────────────────── 200 OK ───────────────────┘
  │
  │        [IdP: SignOut("idp-session", OpenIddict)]
  │        [Wipe .idp.session cookie]
  │        [Revoke refresh tokens in DB]
  │
  │   ◄── 302 → /signout-callback-oidc ────────────────────┘
  │
  ├─④ GET /signout-callback-oidc  ─────────────────► Demo.Bff
  │   ◄── 302 → https://demo.example.com/ ──────────────────┘
  │
  ├─⑤ GET /bff/user  ──────────────────────────────► Demo.Bff
  │   ◄── 401 ───────────────────────────────────────────────┘
  │
  └─⑥ "Login" button shown — all apps logged out
```

---

## Flow 5 — Token refresh (silent, server-side only)

> Access token expires. BFF silently refreshes using stored refresh token.
> Browser is never involved in the token exchange.

```
Demo.Bff (background / next request)
  │
  ├─① Detect access_token expiry from ticket metadata
  │
  ├─② POST /connect/token  ───────────────────────► Auth.IdP
  │        grant_type=refresh_token
  │        refresh_token=<stored server-side>
  │        client_id=demo-login-bff
  │        client_secret=<secret>
  │                                                       │
  │   ◄── new access_token + new refresh_token ───────────┘
  │
  ├─③ Update ticket in TicketStore (replace tokens)
  │
  └─④ Next API call uses new access_token transparently
       (Browser .demo.session cookie unchanged)
```

---

## Verification checklist

| # | Check | How to verify |
|---|-------|--------------|
| 1 | No token in browser storage | DevTools → Application → Local/Session Storage — must be empty |
| 2 | Auth state via cookie only | DevTools → Application → Cookies → only `.demo.session` present |
| 3 | SSO silent login works | Login to Demo, then open CTS — no password prompt |
| 4 | Back-channel logout clears all apps | Logout from Demo, check `/bff/user` on CTS returns 401 |
| 5 | Token introspection enforced | Call API directly (no BFF) without token — must get 401 |
| 6 | Back-channel logout endpoint is wired | POST `/backchannel-logout` with invalid token → 400; valid → 200 |
