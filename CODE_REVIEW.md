# Code Review — TodoList Repository

**Reviewer:** Principal Engineer
**Date:** 2026-03-21
**Last updated:** 2026-03-21
**Scope:** Full codebase (backend API, data access layer, frontend, tests, configuration)

---

## Summary

| Severity | Open | Fixed | Total |
|----------|------|-------|-------|
| 🔴 Critical | 1 | 3 | 4 |
| 🟠 High | 5 | 0 | 5 |
| 🟡 Medium | 16 | 0 | 16 |
| 🔵 Low | 8 | 0 | 8 |
| **Total** | **30** | **3** | **33** |

---

## Issue Table

| # | Severity | Area | File(s) | Issue | Recommendation |
|---|----------|------|---------|-------|----------------|
| 1 | ✅ Fixed | Security — Auth | `ToDoApi/Services/UserService.cs`, `AuthService.cs` | **Weak password hashing**: SHA256 with a GUID salt is used. SHA256 is a fast hash, not designed for passwords. Vulnerable to GPU/ASIC brute-force. | **Fixed 2026-03-21**: Replaced with `Rfc2898DeriveBytes.Pbkdf2` (PBKDF2-SHA512, 100 000 iterations, 32-byte cryptographically random salt, constant-time verify via `CryptographicOperations.FixedTimeEquals`). Tests updated accordingly. |
| 2 | ✅ Fixed | Security — Config | `appsettings.json`, `appsettings.Development.json` | **Hardcoded JWT secret** (`"A-Very-Long-Super-Secret-Key-Replace-In-Production"`) is committed to source control. Anyone with repo access can forge tokens. | **Fixed 2026-03-21**: Removed `Jwt:Key` from both appsettings files. `AuthService.GenerateToken()` now throws `InvalidOperationException` at first use if the key is absent or shorter than 32 chars. Set key via `JWT__Key` env var or `dotnet user-secrets set "Jwt:Key" "…"`. Integration tests inject a test key via `ConfigureAppConfiguration`. |
| 3 | ✅ Fixed | Security — Authorization | `ToDoApi/Controllers/UserController.cs:40` | **Missing authorization check on user update**: `PUT /api/user/{id}` has no validation that `id` matches the authenticated user. Any authenticated user can modify any other user's data by guessing a GUID. | **Fixed 2026-03-21**: `Update` now extracts `ClaimTypes.NameIdentifier` from the JWT and returns `403 Forbid()` if it doesn't match the route `id`. Unit tests updated with `MakeContext(Guid)` helper; new `UpdateReturnsForbidWhenCallerIsNotOwner` and `UpdateReturnsForbidWhenNoClaimsPresent` unit tests added; new `Update_ReturnsForbidden_WhenCallerIsNotOwner` integration test added. |
| 4 | 🔴 Critical | Security — CORS | `appsettings.json`, `ToDoApi/Program.cs:56` | **Empty CORS `AllowedOrigins` in production config** results in all cross-origin requests being silently rejected. This is a misconfiguration that will break the app in production. | Configure production origins via environment variables: `CORS__AllowedOrigins__0=https://app.example.com`. |
| 5 | 🟠 High | Security — Auth | `ToDoApi/Services/AuthService.cs:63` | **JWT expiration set to 7 days**. A stolen token is valid for a full week. There is no revocation mechanism. | Use short-lived access tokens (15–60 min) and implement refresh tokens with rotation and revocation. |
| 6 | 🟠 High | Security — Auth | `ToDoApi/Controllers/AuthController.cs`, `UserController.cs` | **No rate limiting** on `/api/auth/login` or `/api/user` (registration). Allows unlimited brute-force and credential stuffing attacks. | Add `Microsoft.AspNetCore.RateLimiting` middleware with a fixed-window or sliding-window policy on auth endpoints. |
| 7 | 🟠 High | Security — Transport | `ToDoApi/Program.cs:90` | **No HSTS header** configured. HTTPS redirect is only applied outside Development, but `Strict-Transport-Security` is never set. Browsers won't cache the HTTPS requirement. | Add `app.UseHsts()` and configure `HstsOptions` with `MaxAge`, `IncludeSubdomains`, and `Preload`. |
| 8 | 🟠 High | Security — Frontend | `frontend/src/services/authService.ts:47` | **JWT stored in `localStorage`**: accessible to any JavaScript running on the page. If XSS occurs, the token is immediately stolen. | Store tokens in `httpOnly` cookies set by the server. Remove token handling from JavaScript entirely. |
| 9 | 🟠 High | Validation | `ToDoApi/Models/CreateUserRequest.cs`, `UpdateUserRequest.cs` | **No minimum password length**: only `[MaxLength(50)]` is enforced. A 1-character (or even empty) password is accepted. | Add `[MinLength(8)]` (NIST SP 800-63B minimum) and consider a complexity rule or breach check. |
| 10 | 🟡 Medium | Security — XSS | `ToDoApi/Filters/SanitizeStringsFilter.cs` | **HTML-encoding applied at ingress**: strings are stored encoded in the database. Clients must double-decode, and encoding is only safe for HTML context—not JS, CSS, or URL contexts. | Store raw data; apply context-appropriate encoding at the output layer. Vue's template engine already auto-escapes in `{{ }}` interpolation. |
| 11 | 🟡 Medium | Security — Headers | `ToDoApi/Program.cs` | **Missing security headers**: `Content-Security-Policy`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, and `Referrer-Policy` are not set. | Add a security-headers middleware (e.g., `NetEscapades.AspNetCore.SecurityHeaders`) to inject these headers on every response. |
| 12 | 🟡 Medium | Security — CORS | `ToDoApi/Program.cs:60` | **`AllowAnyHeader()` and `AllowAnyMethod()`**: overly permissive. Any custom header and any HTTP verb (including exotic ones) are allowed from configured origins. | Whitelist only the methods and headers actually needed: `GET, POST, PUT, PATCH, DELETE` and `Authorization, Content-Type`. |
| 13 | 🟡 Medium | Authorization | `ToDoApi/Controllers/UserController.cs` | **No delete-account endpoint**: users cannot delete their own accounts. This is a GDPR / right-to-erasure obligation. | Implement `DELETE /api/user/{id}` with the same authorization check as issue #3. |
| 14 | 🟡 Medium | Performance | `ToDoApi/Services/IToDoService.cs`, `ToDoController.cs` | **No pagination** on `GET /api/todo`. All todos for a user are returned in a single unbounded query. This will degrade with volume. | Add `skip` / `take` (or cursor-based) query parameters and return a paged envelope with `totalCount`. |
| 15 | 🟡 Medium | Data Integrity | `ToDo.DataAccess/Repositories/ToDoRepository.cs` | **No concurrency control**: concurrent edits can silently overwrite each other (last-write-wins). | Add a `RowVersion byte[]` column to the `ToDo` entity, configure EF Core concurrency tokens, and return `ETag` headers; enforce `If-Match` on mutations. |
| 16 | 🟡 Medium | Data Integrity | `ToDoApi/Services/UserService.cs:48` | **No uniqueness check on email update**: updating a user's email to one already taken will either silently fail or throw an unhandled database constraint exception. | Before saving, call `GetUserByEmailAsync(request.Email)` and return a 409 Conflict if a different user owns it. |
| 17 | 🟡 Medium | Validation | `ToDoApi/Models/UpdateUserRequest.cs` | **No `[EmailAddress]` attribute on email field** in UpdateUserRequest, unlike CreateUserRequest. Invalid email formats are accepted on update. | Add `[EmailAddress]` to `UpdateUserRequest.Email`. |
| 18 | 🟡 Medium | Testing | `ToDoApi.Tests/Integration/*IntegrationTests.cs` | **Shared in-memory database within a test class**: a new DB name is generated per class, but tests within the class share state. Test execution order can cause false passes or failures. | Generate a unique DB name per test method (move it into each test or use `IAsyncLifetime.InitializeAsync`). |
| 19 | 🟡 Medium | Testing | All test projects | **No authorization boundary tests**: no test verifies that user A cannot read, update, or delete user B's todos or profile. The bug in issue #3 would not be caught. | Add integration tests that authenticate as User A and attempt operations on resources owned by User B; assert 403. |
| 20 | 🟡 Medium | Frontend — Types | `frontend/src/types/user.ts` | **`UserResponse` type declares `createdAt`** which the backend does not return. The field will always be `undefined`, causing silent bugs in any consumer code. | Remove `createdAt` from the frontend type, or have the backend include it in the response DTO. |
| 21 | 🟡 Medium | Configuration | `frontend/.env` | **No `.env.example`** or `.env.production` template**: developers cloning the repo have no reference for required variables, and CI/CD pipelines have no template to fill in. | Add `.env.example` with placeholder values and document each variable; add `.env.production` to `.gitignore`. |
| 22 | 🟡 Medium | Architecture | `ToDo.DataAccess/ToDo.cs`, `User.cs` | **Inconsistent timestamp naming** across entities: `ToDo` uses `CreatedAt`/`UpdatedAt`/`FinishedAt`; `User` uses `CreatedDate`/`LastModifiedDate`. | Standardize to `CreatedAt` / `UpdatedAt` across all entities. Extract into a shared `AuditableEntity` base class. |
| 23 | 🟡 Medium | Architecture | All entities | **No soft deletes**: deletes are permanent with no audit trail. Recovering mistakenly deleted data is impossible. | Add `DeletedAt DateTime?` to entities, implement a global EF Core query filter to exclude deleted rows, and convert delete operations to set `DeletedAt`. |
| 24 | 🟡 Medium | Security — Logging | `ToDoApi/Services/AuthService.cs` | **Security events logged but not structured for alerting**: failed logins are logged as plain warnings with no aggregation or alerting. Rate-limited brute-force patterns are invisible. | Emit structured log events with a dedicated `EventId` for security events. Integrate with an alerting backend (e.g., Application Insights alert rules, Grafana). |
| 25 | 🔵 Low | API Design | All controllers | **No API versioning**: routes are `/api/todo` with no version segment. Breaking changes will require a flag day. | Prefix routes with `/api/v1/` and configure `Asp.Versioning.Http` to support header- or URL-based versioning. |
| 26 | 🔵 Low | API Design | All GET endpoints | **No caching headers**: responses have no `Cache-Control`, `ETag`, or `Last-Modified`. Every client request hits the database. | Return `ETag` on single-resource GETs and `Cache-Control: no-cache` (with ETag validation) on list endpoints. |
| 27 | 🔵 Low | Frontend — UX | `frontend/src/views/LoginView.vue:44` | **Login label says "Username"** but the field is the user's email address. Backend `AuthRequest` uses `Email`. This is misleading to end users. | Change the label to "Email" or "Email address". |
| 28 | 🔵 Low | Frontend — UX | `frontend/src/views/RegisterView.vue` | **No password strength feedback**: users receive no real-time guidance on password quality and may unknowingly create weak (but technically valid) passwords. | Add a client-side strength meter and display minimum-length requirement inline. |
| 29 | 🔵 Low | Frontend — Router | `frontend/src/router/index.ts` | **Route names are stringly-typed** (`'home'`, `'login'`). Typos cause silent navigation failures at runtime. | Use a typed route enum or object constant (`Routes.Home`, `Routes.Login`) so mistyped names are caught at compile time. |
| 30 | 🔵 Low | Frontend — UX | `frontend/src/App.vue:13` | **No guard against duplicate redirect on logout**: the unauthorized event handler calls `router.push('/login')` unconditionally. If the user is already on `/login`, Vue Router emits a `NavigationDuplicated` warning. | Check `router.currentRoute.value.path !== '/login'` before pushing. |
| 31 | 🔵 Low | Documentation | `README.md` | **README lacks production deployment instructions**: no documentation on required environment variables, secret injection, database migrations, or CORS configuration for production. | Add a "Production Setup" section covering environment variables, migration commands, and a security checklist. |
| 32 | 🔵 Low | API Design | `ToDoApi/Program.cs` | **No global request body size limit**: large payloads can exhaust memory. No `[RequestSizeLimit]` or global `KestrelServerOptions.Limits.MaxRequestBodySize` configuration. | Set a global maximum (e.g., 1 MB) via `builder.WebHost.ConfigureKestrel(...)` and override per endpoint where larger bodies are needed. |
| 33 | 🔵 Low | API Design | `ToDoApi/Program.cs` | **OpenAPI/Scalar only enabled in Development**, but no exported OpenAPI spec file exists in the repo. External teams and frontend developers have no contract reference. | Run `dotnet swagger tofile` in CI and commit the generated `openapi.json`. Consider a public staging docs endpoint. |

---

## Top 5 Fixes by Priority

| # | Status | Issue | Why it matters |
|---|--------|-------|----------------|
| 1 | ✅ Done | Replace SHA256 with PBKDF2 | SHA256 can be brute-forced at ~10B/sec on a GPU; PBKDF2 with 100k iterations caps that at ~10k/sec |
| 2 | ✅ Done | Remove hardcoded JWT secret | Anyone who has ever cloned the repo can forge authentication tokens |
| 3 | ✅ Done | Add ownership check to `UserController.Update` | Any authenticated user could overwrite any other user's profile by guessing a GUID |
| 4 | 🔴 Open | Add rate limiting to auth endpoints | Without it, brute-force and credential-stuffing attacks are unconstrained |
| 5 | 🟠 Open | Move JWT from `localStorage` to `httpOnly` cookie | Closes the token-theft-via-XSS attack surface entirely |
