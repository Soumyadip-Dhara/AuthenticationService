# SSO Auth Flow Diagrams

Visual companion to [AUTH-FLOWS.md](AUTH-FLOWS.md). Render in any Mermaid-aware
viewer (VS Code Markdown preview, GitHub, etc.).

---

## 1. Component / topology map

```mermaid
flowchart LR
    subgraph Browser
        AuthNg["Auth.Angular<br/>(login page)"]
        CtsNg["Cts.Angular<br/>:4200"]
        BillNg["Billing.Angular"]
        DemoNg["Demo.Angular<br/>:4300"]
    end

    subgraph IdP["Auth.IdP :5001"]
        IdPcookie[".idp.session cookie<br/>= the SSO"]
        Authorize["/connect/authorize"]
        Token["/connect/token"]
        Introspect["/connect/introspect"]
        Logout["/connect/logout"]
        Dispatcher["BackchannelLogoutDispatcher"]
    end

    subgraph CTS["CTS"]
        CtsBff["Cts.Bff :5002<br/>confidential client + YARP"]
        CtsApi["Cts.Api :6002<br/>resource server"]
    end

    subgraph Billing["Billing"]
        BillBff["Billing.Bff :5003"]
        BillApi["Billing.Api :6003"]
    end

    subgraph Demo["Demo"]
        DemoBff["Demo.Bff :5004<br/>confidential client + YARP"]
        DemoApi["Demo.Api :6004<br/>resource server"]
    end

    CtsNg  -->|opaque .cts.session cookie|     CtsBff
    BillNg -->|opaque .billing.session cookie|  BillBff
    DemoNg -->|opaque .demo.session cookie|     DemoBff
    AuthNg --> Authorize

    CtsBff  -->|"****** (server-side)"| CtsApi
    BillBff -->|"****** (server-side)"| BillApi
    DemoBff -->|"****** (server-side)"| DemoApi

    CtsBff  -.->|code/refresh exchange| Token
    BillBff -.->|code/refresh exchange| Token
    DemoBff -.->|code/refresh exchange| Token

    CtsApi  -.->|validate token| Introspect
    BillApi -.->|validate token| Introspect
    DemoApi -.->|validate token| Introspect

    Logout --> Dispatcher
    Dispatcher -.->|logout_token POST| CtsBff
    Dispatcher -.->|logout_token POST| BillBff
    Dispatcher -.->|logout_token POST| DemoBff
```

---

## 2. First login — no existing session (Demo app)

```mermaid
sequenceDiagram
    autonumber
    participant B   as Browser
    participant Ng  as Demo.Angular
    participant Bff as Demo.Bff :5004
    participant IdP as Auth.IdP :5001
    participant ANg as Auth.Angular
    participant PG  as PostgreSQL

    B->>Ng:  open https://localhost:4300/
    Ng->>Bff: GET /bff/user
    Bff-->>Ng: 401 (no .demo.session)
    Ng->>B:  redirect to /login

    B->>Bff: GET /bff/login
    Note over Bff: Challenge("oidc")<br/>build PKCE verifier + challenge
    Bff-->>B: 302 → /connect/authorize?code_challenge=...

    B->>IdP: GET /connect/authorize
    Note over IdP: AuthenticateAsync("idp-session")<br/>no cookie → Challenge
    IdP-->>B: 302 → /Account/Login

    B->>ANg:  load login form
    ANg->>IdP: POST /Account/LoginApi {user, pass}
    IdP->>PG:  UserStore.ValidateAsync (bcrypt)
    PG-->>IdP: user ok
    Note over IdP: build idp-session identity<br/>mint sid = Guid.NewGuid()<br/>SignInAsync → Set-Cookie .idp.session
    IdP-->>ANg: { returnUrl: /connect/authorize?... }
    ANg->>B:  window.location = returnUrl

    B->>IdP: GET /connect/authorize (with .idp.session)
    Note over IdP: session valid → ClaimsBuilder<br/>load user, copy sid, SetResources("demo-api")<br/>SignIn(OpenIddict) → issue code
    IdP-->>B: 302 → /signin-oidc?code=...

    B->>Bff: GET /signin-oidc?code=...
    Bff->>IdP: POST /connect/token (code + PKCE + secret)
    IdP-->>Bff: access + id + refresh tokens
    Note over Bff: SaveTokens → InMemoryTicketStore<br/>key→ticket, sid→SidIndex<br/>Set-Cookie .demo.session=<random>
    Bff-->>B: 302 → frontend origin

    B->>Ng:  reload
    Ng->>Bff: GET /bff/user (with .demo.session)
    Bff-->>Ng: 200 user claims
    Note over Ng: guard passes → dashboard renders
```

---

## 3. Silent second login = the actual SSO (CTS after Demo)

```mermaid
sequenceDiagram
    autonumber
    participant B   as Browser
    participant Bff as Cts.Bff :5002
    participant IdP as Auth.IdP :5001

    B->>Bff: open CTS → GET /bff/login
    Bff-->>B: 302 → /connect/authorize (PKCE)

    B->>IdP: GET /connect/authorize<br/>(browser auto-sends .idp.session)
    Note over IdP: AuthenticateAsync("idp-session") succeeds first try<br/>NO login UI rendered
    IdP-->>B: 302 → /signin-oidc?code=...

    B->>Bff: GET /signin-oidc?code=...
    Bff->>IdP: POST /connect/token
    IdP-->>Bff: tokens (aud=cts-api, scope=api:cts)
    Note over Bff: Set-Cookie .cts.session
    Bff-->>B: 302 → frontend

    Note over B,IdP: User never saw a login screen for CTS
```

---

## 4. API call — introspection (cache miss path)

```mermaid
sequenceDiagram
    autonumber
    participant B   as Browser
    participant Bff as Demo.Bff :5004
    participant Api as Demo.Api :6004
    participant IdP as Auth.IdP :5001

    B->>Bff: GET /api/resource (.demo.session)
    Note over Bff: cookie → ticket → principal<br/>RequireAuthorization passes
    Note over Bff: YARP transform: strip cookies,<br/>GetTokenAsync("access_token")
    Bff->>Api: GET /api/resource<br/>Authorization: ******

    Note over Api: dual scheme: peek iss<br/>iss ≠ legacy-auth → introspection
    Api->>Api: SHA256(token) cache lookup
    alt cache miss
        Api->>IdP: POST /connect/introspect (token + client creds)
        IdP-->>Api: { active, sub, aud, scope, exp }
        Note over Api: aud must contain demo-api<br/>scope must contain api:demo<br/>cache until min(exp, MaxCacheTtl)
    else cache hit
        Note over Api: served from in-memory cache
    end
    Note over Api: build principal → [Authorize] passes
    Api-->>Bff: 200 resource data
    Bff-->>B:  200 (JSON)
```

---

## 5. Single logout — back-channel fan-out

```mermaid
sequenceDiagram
    autonumber
    participant B       as Browser
    participant DemoBff as Demo.Bff :5004
    participant IdP     as Auth.IdP :5001
    participant CtsBff  as Cts.Bff  :5002
    participant BillBff as Billing.Bff :5003

    B->>DemoBff: GET /bff/logout
    Note over DemoBff: SignOut("demo-cookie","oidc")<br/>remove ticket, expire .demo.session
    DemoBff-->>B: 302 → /connect/logout?id_token_hint=...

    B->>IdP: GET /connect/logout
    Note over IdP: parse id_token_hint → sub + sid<br/>(fallback: .idp.session principal)

    par Back-channel fan-out (server-to-server, no browser)
        IdP->>CtsBff: POST /backchannel-logout {logout_token RS256}
        Note over CtsBff: validate iss/aud/sig/events<br/>SidIndex.GetSessions(sid)<br/>RemoveAsync each → Clear(sid)
        CtsBff-->>IdP: 200
    and
        IdP->>BillBff: POST /backchannel-logout {logout_token RS256}
        Note over BillBff: validate + clear billing sessions for sid
        BillBff-->>IdP: 200
    and
        IdP->>DemoBff: POST /backchannel-logout (idempotent)
        DemoBff-->>IdP: 200
    end

    Note over IdP: SignOut("idp-session", OpenIddict)<br/>wipe .idp.session, revoke refresh tokens
    IdP-->>B: 302 → /signout-callback-oidc

    B->>DemoBff: GET /signout-callback-oidc
    DemoBff-->>B: 302 → frontend
    Note over B: /bff/user → 401 → "Login" shown
```

---

## 6. Token cryptography — signed vs encrypted

The decision tree for every token the IdP mints:

```mermaid
flowchart TD
    Q{"Does anyone other than<br/>the IdP read the inside<br/>of this token?"}

    Q -->|Yes| SignOnly["Sign only<br/>(JWT: header.payload.signature)<br/>verifiable via JWKS"]
    Q -->|No|  Both["Encrypt + sign<br/>(JWE: 5 parts, opaque blob)<br/>only IdP can open"]

    SignOnly --> IdToken["id_token<br/>BFF reads sub/sid/name<br/>+ id_token_hint at logout"]
    SignOnly --> LogoutToken["logout_token<br/>receiving BFF extracts sid"]

    Both --> Access["access_token<br/>API cannot decode → introspection"]
    Both --> Refresh["refresh_token<br/>BFF uses at refresh"]
    Both --> Code["authorization code<br/>BFF forwards to /connect/token"]

    style SignOnly fill:#dfe,stroke:#3c9
    style Both    fill:#fed,stroke:#e83
```

### 6.1 The two IdP certificates

```mermaid
flowchart LR
    subgraph IdPcerts["Auth.IdP"]
        SignCert["Signing cert<br/>RS256<br/>private key: IdP only"]
        EncCert["Encryption cert<br/>RSA-OAEP + A256GCM<br/>private key: IdP only"]
    end

    SignCert -->|public key published| JWKS["/.well-known/openid-configuration/jwks"]
    EncCert  -.->|public key NOT published| X["(nobody encrypts for the IdP)"]

    JWKS --> Verify["BFF verifies id_token<br/>+ logout_token signatures"]

    SignCert --> AllTokens["signs EVERY token"]
    EncCert  --> EncTokens["also encrypts access /<br/>refresh / auth-code"]

    style SignCert fill:#dfe,stroke:#3c9
    style EncCert  fill:#fed,stroke:#e83
```

### 6.2 Why the API must introspect

```mermaid
flowchart LR
    A["access_token<br/>= JWE (encrypted blob)"] -->|API cannot decrypt locally| B["POST /connect/introspect<br/>token + client creds"]
    B --> C["IdP: decrypt → verify sig →<br/>check revocation in DB"]
    C --> D["{ active, sub, aud, scope, exp, ... }"]
    D --> E["API caches by SHA256(token)<br/>until min(exp, MaxCacheTtl)"]

    A -. "alternative: DisableAccessTokenEncryption()" .-> F["plain signed JWT<br/>validate locally via JWKS<br/>(faster, no instant revoke)"]

    style A fill:#fed,stroke:#e83
    style F fill:#eef,stroke:#88c
```

---

## 7. The `sid` thread — why logout works

```mermaid
flowchart LR
    A["Login<br/>AccountController<br/>sid = Guid.NewGuid()"] --> B[".idp.session cookie"]
    B --> C["ClaimsBuilder<br/>copy sid → OIDC principal"]
    C --> D["id_token + access_token<br/>carry sid"]
    D --> E["BFF SidIndex<br/>sid → sessionKeys"]
    A --> F["logout_token<br/>carries sub + sid"]
    F --> G["BFF GetSessions(sid)<br/>→ revoke tickets"]
    E --> G

    style A fill:#fde,stroke:#c39
    style G fill:#dfe,stroke:#3c9
```

> If `sid` breaks anywhere in this chain, back-channel logout silently fails —
> one app logs out but other SSO sessions remain active.
