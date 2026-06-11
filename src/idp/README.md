# src/idp

Identity Provider service boundary.

Responsibilities:

- OIDC/OAuth2 endpoints (`authorize`, `token`, `logout`, `introspect`)
- SSO session cookie issuance
- Back-channel logout token dispatch
- Client/application registration via configuration
