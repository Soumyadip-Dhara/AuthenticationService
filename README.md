# AuthenticationService

A first-step, configuration-driven blueprint for a reusable authentication platform with:

- OpenID Connect compliant Identity Provider (IdP)
- BFF (Backend for Frontend) pattern so tokens stay server-side
- SSO across multiple applications
- Clear microservice-ready folder structure

## Repository layout

```text
.
├── apps/
│   ├── demo-login/               # Demo app login contract and route expectations
│   └── sample-app/                # Example consumer application integration notes
├── configs/
│   └── applications.example.yaml  # Per-application OIDC/BFF registration model
├── docs/
│   ├── AUTH-FLOWS.md              # Detailed auth/SSO/logout flow definitions
│   ├── DEMO-LOGIN.md              # Demo app login walkthrough and checklist
│   └── SSO-AUTH-FLOW-DIAGRAMS.md  # Mermaid visual diagrams for the flows
└── src/
    ├── bff/                       # BFF service boundary (per-app adapters can be added)
    ├── idp/                       # Identity Provider service boundary
    └── shared/                    # Shared contracts/configuration models
```

## First step outcome

This step establishes architecture, integration contracts, and flow documentation so application teams can integrate by configuration.

Start with:

1. `docs/AUTH-FLOWS.md`
2. `docs/DEMO-LOGIN.md`
3. `docs/SSO-AUTH-FLOW-DIAGRAMS.md`
4. `configs/applications.example.yaml`
