# ToDo List Application

This project is a full-stack ToDo application featuring a **C# ASP.NET Core Web API** backend and a **Vue 3 + TypeScript** frontend.

## Project Structure

- `ToDoApi`: The REST API backend handling routing and controllers.
- `ToDo.DataAccess`: The data access layer utilizing Entity Framework Core.
- `frontend`: The modern Vue 3 interface styled with scoped CSS.
- `ToDoApi.Tests` / `ToDo.DataAccess.Tests`: Automated backend unit testing frameworks.

---

## How to Run the Application

To experience the full functionality of the application, you must run **both** the API and the frontend development server concurrently. 

> **Note:** The Vue application uses Vite's proxy feature to automatically route all interface requests starting with `/api` to the C# backend. This prevents CORS policy issues.

### 1. Start the Backend API

Open a terminal, navigate to the `ToDoApi` directory, and run the project:

```bash
cd ToDoApi
dotnet run
```

*The backend API will initialize and actively listen on `http://localhost:5128`.*

### 2. Start the Vue Frontend

Open a **second terminal window**, navigate to the `frontend` directory, install Node packages (if it's your first time), and launch the server:

```bash
cd frontend
npm install
npm run dev
```

*The built-in Vite server will start and provide a local interface URL, typically `http://localhost:5173` (or `http://localhost:5175` if the default port is in use).*

### 3. View the App!

Open your web browser and navigate to the frontend URL provided in step 2. You will now be able to add, edit, finish, and delete ToDo tasks while the data synchronizes robustly with the API perfectly.

---

## Running Automated Tests

### Backend Unit Tests (C# xUnit)
To verify the accuracy and integrity of the API endpoints, use the `dotnet test` command:
```bash
cd ToDoApi.Tests
dotnet test
```

### Frontend Unit Tests (Vitest)
To verify Vue component functionalities logic and DOM rendering rules:
```bash
cd frontend
npm run test:unit
```

### Frontend E2E testing (Playwright)
*(Requires the frontend and backend servers to be running simultaneously)*
To simulate full end-to-end user browser interactions, trigger Playwright checks:
```bash
cd frontend
npx playwright test
```
*(Optionally, you can append `--project=chromium` if you only wish to test the Chrome engine).*

---

## Assumptions

The following assumptions were made during the design and implementation of this project.

### Authentication and identity
- One account per email address. Email is treated as the stable, unique identifier for a user; display name is mutable.
- Passwords are hashed with PBKDF2-SHA512 (100 000 iterations, 32-byte random salt). No third-party identity provider (OAuth, OIDC) is assumed to be available.
- JWTs are short-lived (1 hour) and delivered via `httpOnly SameSite=Strict` cookies. There is currently no refresh-token mechanism, so users must re-authenticate after expiry.

### Data ownership
- Every todo belongs to exactly one user and is invisible to all other users. There is no concept of shared or public lists in the current implementation.
- Users may only modify or delete their own account and their own tasks. The API enforces this at the controller level on every mutating endpoint.

### Infrastructure
- The application runs as a single process behind a TLS-terminating reverse proxy (nginx, Caddy, etc.). The API itself does not handle TLS directly; HTTPS is enforced by redirecting HTTP traffic and by HSTS headers.
- The in-memory EF Core database is intentional for this stage of the project — it keeps the setup zero-dependency and lets tests run without a real database engine. All data is lost on restart.
- Rate limiting is applied per source IP. It is assumed that a reverse proxy correctly forwards the real client IP (via `X-Forwarded-For` or `X-Real-IP`) so limits are not accidentally applied to the proxy's address.

### Frontend
- The Vue SPA is served as a static build from a CDN or a web server. The Vite dev proxy (`/api` → backend) is a development convenience only and is not part of any production deployment.
- Users have JavaScript enabled. The application has no server-rendered or no-JS fallback.
- The browser supports `httpOnly` cookies and the Fetch Credentials API (`credentials: 'include'`). All major modern browsers satisfy this requirement.

### Scale
- The application is designed for personal or small-team use. See the [Scalability](#scalability) section for the path to handle larger loads.

---

## Scalability

The following describes how the application can scale horizontally without touching the database layer.

### Stateless API
The API is fully stateless — all session state is carried in the signed JWT cookie. Any number of API instances can run behind a load balancer without sticky sessions or shared in-process caches. Adding instances is a matter of increasing the replica count in the container orchestrator (Kubernetes, ECS, etc.) and pointing the load balancer at the new pods.

### JWT signing key distribution
Because the JWT is validated using a symmetric key (`JWT__Key`), every instance must share the same secret. This is already satisfied by injecting the key via environment variable or a secrets manager (Vault, AWS Secrets Manager, Azure Key Vault). No instance-local state is involved.

### Rate limiting
The current per-IP sliding-window rate limiter runs in-process. Across multiple instances, each instance tracks its own window independently, so the effective limit multiplies by the number of replicas. To enforce a true global limit, replace the in-process policy with a distributed rate limiter backed by Redis (e.g. `RedisRateLimiter` from `Microsoft.AspNetCore.RateLimiting` or a community Redis sliding-window policy).

### SignalR backplane (future)
When real-time collaboration is introduced, SignalR connections are instance-local by default. To fan out a message from one instance to clients connected to other instances, a backplane is required. The standard choice is the [Azure SignalR Service](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-overview) or the open-source Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`). Adding either requires a single `builder.Services.AddSignalR().AddAzureSignalR(...)` or `.AddStackExchangeRedis(...)` call — no changes to hub or client code.

### Caching
Read-heavy endpoints (`GET /api/todo`) can be fronted by an output cache or a CDN edge cache. With a short TTL (e.g. 5 seconds) and cache keys scoped to the authenticated user ID, most list fetches will never reach the API process. ASP.NET Core's built-in output caching middleware (`builder.Services.AddOutputCache()`) supports Redis as a distributed store, making it cluster-safe.

### Horizontal pod autoscaling
Because all state is in the JWT and the database, the API pod has no warm-up dependencies beyond the database connection. Kubernetes HPA can scale on CPU or custom metrics (e.g. active WebSocket connections for the SignalR hub) and new pods are ready to serve traffic immediately.

### Frontend CDN
The compiled Vue SPA is a set of static files with content-hash filenames. It can be pushed to any CDN (Cloudflare, CloudFront, Azure Static Web Apps, etc.) and served from the edge closest to the user with no API involvement.

---

## Production Setup

### Required environment variables

#### Backend (`ToDoApi`)

| Variable | Description | Example |
|----------|-------------|---------|
| `JWT__Key` | JWT signing secret — minimum 32 characters, never commit to source control | `openssl rand -base64 32` |
| `CORS__AllowedOrigins__0` | Allowed frontend origin (repeat with `__1`, `__2` for multiple) | `https://app.example.com` |

Set via shell environment or a secrets manager:
```bash
export JWT__Key="$(openssl rand -base64 32)"
export CORS__AllowedOrigins__0="https://app.example.com"
dotnet run --environment Production
```

Or via `dotnet user-secrets` for local development:
```bash
cd ToDoApi
dotnet user-secrets set "Jwt:Key" "<32+ char secret>"
```

#### Frontend (`frontend`)

Copy `.env.example` to `.env.production` and fill in values:
```bash
cp frontend/.env.example frontend/.env.production
# Edit .env.production — set VITE_API_BASE_URL to your production API URL
```

> `.env.production` is listed in `.gitignore` — never commit it.

### HTTPS

- The API enforces HTTPS redirection and HSTS (`Strict-Transport-Security: max-age=31536000; includeSubDomains`) in non-Development environments.
- Terminate TLS at a reverse proxy (nginx, Caddy, Azure Front Door, etc.) and forward to the Kestrel process.
- Once your domain is stable, consider adding it to the [HSTS preload list](https://hstspreload.org) and setting `options.Preload = true` in `Program.cs`.

### Database

The current implementation uses EF Core's **in-memory database** (suitable for demos only). For production:
1. Replace `UseInMemoryDatabase` in `Program.cs` with a real provider (e.g. `UseSqlServer`, `UseNpgsql`).
2. Add the corresponding NuGet package.
3. Run `dotnet ef database update` to apply migrations.

### OpenAPI spec

The project generates `ToDoApi/openapi.json` at build time via `Microsoft.Extensions.ApiDescription.Server`. Commit this file so frontend teams and CI pipelines always have an up-to-date contract:

```bash
cd ToDoApi
dotnet build          # produces / updates openapi.json
git add openapi.json
```

The interactive Scalar UI is available at `/scalar/v1` in Development mode only.

---

## Future

### Collaboration — Shared Todo Lists with SignalR
Users will be able to create shared lists and invite other users to collaborate. Changes made by any participant (create, edit, complete, delete) will be broadcast in real time via a SignalR hub so every connected client sees the update without polling. Live presence indicators will show which users are currently viewing or editing a task.

### Observability — Expanded Logs and Telemetry
Structured logging will be extended across all services with consistent `EventId` constants, request correlation IDs, and enriched context (user ID, request path, duration). Slow queries, unexpected 5xx responses, and business-level events (task created, user registered) will each have dedicated log entries that can be forwarded to any structured log backend.

### OpenTelemetry Integration
The application will be instrumented with the [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/net/) to emit traces, metrics, and logs through a single unified pipeline. This enables drop-in compatibility with any OTLP-compatible backend (Jaeger, Tempo, Datadog, Azure Monitor, etc.) and end-to-end distributed tracing across the API and data access layers — without changing business logic.

### Authentication — Refresh Token Rotation
Short-lived access tokens (current: 1 hour) will be complemented by long-lived refresh tokens stored in a separate `httpOnly` cookie. Refresh tokens will be rotated on every use and stored in the database for revocation. A `POST /api/auth/refresh` endpoint will issue a new access token silently, keeping users logged in without re-entering credentials.

### Todo Organisation — Labels, Due Dates and Priorities
Tasks will support optional metadata: colour-coded labels, a due date with optional reminder, and a priority level (low / medium / high). The frontend will allow filtering and sorting by any combination of these fields.

### Pagination and Search
The `GET /api/todo` endpoint will support cursor-based pagination and a server-side full-text search parameter so large lists remain performant and the client never has to download all tasks to filter them.

### Notifications
Users will be able to opt in to browser push notifications (via the Push API and a service worker) or email reminders for tasks with approaching due dates.

### Persistent Database
Replace the in-memory EF Core provider with a real relational database (PostgreSQL or SQL Server), add EF Core migrations, and introduce soft deletes with a full audit trail so no data is ever permanently lost without a retention-policy grace period.

### CI/CD Pipeline
A GitHub Actions workflow will run the full test suite, build the Docker images for the API and frontend, publish the generated `ToDoApi.json` OpenAPI spec as a workflow artefact, and deploy to the target environment on merge to `main`.
