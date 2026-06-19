# Microservices Configuration & Deployment Guide

This document outlines all the necessary configuration changes required when moving the application suite to a new machine, handing it over to another developer, or deploying it to a production environment.

## Overview of Components
- **Identity Provider (IDP)**: Central authentication server (Port `5001`).
- **User Management (UM)**: API (`6003`), BFF (`5003`), UI (`4100`).
- **Module Management (MM)**: API (`6006`), BFF (`5006`), UI (`4500`).
- **RabbitMQ**: Event bus / Message broker.
- **PostgreSQL**: Relational database.

---

## 1. Identity Provider (IDP)
**Path:** `AuthenticationService/src/idp/AuthService.IdP/appsettings.json`

| Key | Description | Production / Handoff Change |
|-----|-------------|-----------------------------|
| `ConnectionStrings:DefaultConnection` | Database connection string. | Update Host IP, Port, and Credentials. |
| `Issuer` | Publicly accessible URL of the IDP. | Change to the public domain/IP (e.g., `https://idp.yourdomain.com`). |
| `Deployment:BffHost` & `UiHost` | Base URLs used to seed OAuth `RedirectUris`. | Set to the domain/IP where the BFFs and UIs are hosted. |
| `BackchannelLogout:Clients` | URIs where IDP sends logout notifications. | Update IPs/domains to match the BFFs. |
| `Kestrel:Endpoints:Https:Url` | Internal bind address. | Keep `0.0.0.0:5001` to listen on all interfaces. |
| `NotificationService` | API URL for SMS/Email OTP. | Update to production notification provider (if enabled). |

> **Important Note for Database Seeding:**
> The IDP registers allowed Redirect URIs in the database using `SeedData.cs`. If you change `Deployment:BffHost` or `Deployment:UiHost`, you must **clear the `OpenIddictApplications` table** in your Postgres database so that Entity Framework runs the seed script again on startup and updates the Redirect URIs.

---

## 2. BFF Services (User Management & Module Management)
**Paths:** 
- `UserManagement.bff/appsettings.json`
- `ModuleManagement.bff/appsettings.json`

| Key | Description | Production / Handoff Change |
|-----|-------------|-----------------------------|
| `FrontendUrl` | The URL of the corresponding Angular UI. | Change to the production UI domain or new IP. |
| `Oidc:Authority` | The public URL of the IDP. | **Must exactly match** the IDP's `Issuer`. |
| `ReverseProxy:Clusters:*:Address` | Internal URL of the backing API. | Point to the production API (e.g., `https://127.0.0.1:6003` or container name). |
| `Kestrel:Endpoints:Https:Url` | Internal bind address. | Keep `0.0.0.0` with the appropriate port. |

---

## 3. API Services (User Management & Module Management)
**Paths:** 
- `UserManagement.api/appsettings.json`
- `ModuleManagement.api/appsettings.json`

| Key | Description | Production / Handoff Change |
|-----|-------------|-----------------------------|
| `ConnectionStrings` | Database connections. | Update Host IP, DB Name, and Credentials. |
| `OpenIddict:Jwt:Issuer` | The public URL of the IDP. | **Must exactly match** the IDP's `Issuer`. |
| `RabbitMQConnection` | Message broker connection. | Update Host, Username, and Password. |

---

## 4. Frontend Angular UIs (User Management & Module Management)
**Paths:**
- `angularV20ReusableTemplate/src/environments/environment.ts` (and `.production.ts`)
- `ModuleManagement.UI/src/environments/environment.ts` (and `.production.ts`)

| File | Change Required |
|------|-----------------|
| `environment.ts` | Update `apiBaseUrl` and `bffUrl` to point to the respective BFF's public URL/IP. |
| `auth.interceptor.ts` | Ensure `bffHost` accurately matches the domain/IP of the BFF. This is critical for appending `X-CSRF` headers. |
| `auth.ts` (Service) | If `bffUrl` is hardcoded here, update it or map it to `environment.bffUrl`. |

---

## 5. Deployment Checklist
1. **Database Readiness:** Ensure PostgreSQL is reachable and credentials are correct across all 5 `appsettings.json` files.
2. **RabbitMQ Readiness:** Ensure RabbitMQ is reachable by both APIs.
3. **Ports & Firewall:** Ensure the following ports are open if accessing externally:
   - **IDP:** `5001`
   - **UIs:** `4100`, `4500`
   - **BFFs:** `5003`, `5006` (Optional: expose these via a reverse proxy like Nginx/IIS).
   - *(APIs 6003/6006 do not need to be exposed publicly, only the BFF needs to reach them).*
4. **SSL / Certificates:** In Production, replace the local `.NET Developer Certificates` with valid SSL certificates. Configure Kestrel to use these via `appsettings.json` or standard ASP.NET environment variables (`ASPNETCORE_Kestrel__Certificates__Default__Path`).
5. **CORS:** Ensure that the IDP allows cross-origin requests from the BFFs (this is handled via the seeded Client configurations in `SeedData.cs`).
