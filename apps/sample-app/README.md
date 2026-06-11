# apps/sample-app

Placeholder integration target for demonstrating app onboarding.

Expected auth behavior:

1. Use `/bff/login` to authenticate users.
2. Call `/bff/user` to check session state.
3. Call application APIs through BFF routes.
4. Use `/bff/logout` for local + SSO logout.
