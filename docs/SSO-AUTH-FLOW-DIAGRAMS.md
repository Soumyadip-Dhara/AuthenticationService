# SSO Auth Flow Diagrams

Visual companion to [AUTH-FLOWS.md](AUTH-FLOWS.md). Render in any Mermaid-aware viewer.

---

## 1. Component / topology map

```mermaid
flowchart LR
    subgraph Browser
        AuthNg["Auth UI"]
        AppNg["Consumer App UI"]
    end

    subgraph IdP["Auth.IdP"]
        IdPcookie[".idp.session cookie"]
        Authorize["/connect/authorize"]
        Token["/connect/token"]
        Introspect["/connect/introspect"]
        Logout["/connect/logout"]
        Dispatcher["BackchannelLogoutDispatcher"]
    end

    subgraph App["Consumer Application"]
        AppBff["App.Bff (confidential client)"]
        AppApi["App.Api (resource server)"]
    end

    AppNg -->|opaque app session cookie| AppBff
    AuthNg --> Authorize

    AppBff -->|Authorization header from server-side token| AppApi
    AppBff -.->|code/refresh exchange| Token
    AppApi -.->|validate token| Introspect

    Logout --> Dispatcher
    Dispatcher -.->|logout_token POST| AppBff
```

---

## 2. First login — no existing session

```mermaid
sequenceDiagram
    autonumber
    participant B as Browser
    participant Ng as App UI
    participant Bff as App.Bff
    participant IdP as Auth.IdP

    B->>Ng: Open app
    Ng->>Bff: GET /bff/user
    Bff-->>Ng: 401
    Ng->>B: Redirect to /bff/login

    B->>Bff: GET /bff/login
    Bff-->>B: 302 -> /connect/authorize?code_challenge=...

    B->>IdP: GET /connect/authorize
    IdP-->>B: 302 -> /Account/Login
    B->>IdP: POST /Account/LoginApi
    IdP-->>B: Set-Cookie .idp.session + redirect

    B->>Bff: GET /signin-oidc?code=...
    Bff->>IdP: POST /connect/token
    IdP-->>Bff: access + id + refresh token
    Bff-->>B: Set-Cookie .app.session + 302 frontend
```

---

## 3. SSO login in another application

```mermaid
sequenceDiagram
    autonumber
    participant B as Browser
    participant Bff as AnotherApp.Bff
    participant IdP as Auth.IdP

    B->>Bff: GET /bff/login
    Bff-->>B: 302 -> /connect/authorize
    B->>IdP: GET /connect/authorize (with .idp.session)
    IdP-->>B: 302 -> /signin-oidc?code=...
    B->>Bff: GET /signin-oidc?code=...
    Bff->>IdP: POST /connect/token
    IdP-->>Bff: tokens
    Bff-->>B: Set-Cookie .another.session
```

---

## 4. Single logout (back-channel)

```mermaid
sequenceDiagram
    autonumber
    participant B as Browser
    participant Bff as App.Bff
    participant IdP as Auth.IdP
    participant Other as OtherApp.Bff

    B->>Bff: GET /bff/logout
    Bff-->>B: 302 -> /connect/logout
    B->>IdP: GET /connect/logout

    par Back-channel fan-out
        IdP->>Other: POST /backchannel-logout (logout_token)
        Other-->>IdP: 200
    and
        IdP->>Bff: POST /backchannel-logout (idempotent)
        Bff-->>IdP: 200
    end

    IdP-->>B: 302 -> /signout-callback-oidc
```
