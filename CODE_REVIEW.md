# Code Review — TodoList Application

> Reviewed on 2026-03-20 by principal-level engineer audit.

---

## Issues Table

| # | File | Line | Category | Severity | Issue | Suggested Fix |
|---|------|------|----------|----------|-------|---------------|
| 1 | `ToDoRepository.cs` | 48 | Performance | Medium | `GetByIdAsync` called inside transaction scope — read-only ops don't need a transaction | Move `GetByIdAsync` call outside the transaction |
| 2 | `ToDoRepository.cs` | 64-76 | Performance | Medium | `ChangeStatusAsync` uses a transaction around a simple update, unnecessary overhead | Wrap transactions conditionally based on DB provider |
| 3 | `ToDoRepository.cs` | 31-79 | Error Handling | High | No try/catch around `SaveChangesAsync`, `BeginTransactionAsync`, `CommitAsync` — failures leave state undefined | Catch `DbUpdateException` / `DbUpdateConcurrencyException` |
| 4 | `ToDoRepository.cs` | 47, 64 | Error Handling | High | No explicit rollback on exception — relying on implicit disposal is unclear and undocumented | Add `await transaction.RollbackAsync()` in catch blocks |
| 5 | `ToDoRepository.cs` | 45-60 | Performance | Medium | Delete fetches the entity first, then deletes — two DB round trips | Use `ExecuteDeleteAsync()` (EF Core 7+) for a single query |
| 6 | `ToDoService.cs` | 10-73 | Error Handling | High | All repo exceptions propagate uncaught with no logging or retry logic | Add exception handling; consider Polly for transient failures |
| 7 | `ToDoService.cs` | 30-73 | Code Quality | Low | `DateTime.UtcNow` called inline — untestable and inconsistent across calls | Inject `ISystemClock` or `TimeProvider` via DI |
| 8 | `ToDoService.cs` | 26-36 | Error Handling | Medium | Service has no input validation — malicious clients bypassing model-binding can create invalid records | Validate `string.IsNullOrWhiteSpace(todo.Title)` before persisting |
| 9 | `ToDoController.cs` | 13-62 | Documentation | Low | `ProducesResponseType` attributes missing `500 InternalServerError` | Add `[ProducesResponseType(500)]` to all action methods |
| 10 | `SanitizeStringsFilter.cs` | 26-43 | Code Quality | Low | `prop.GetValue(obj)` called without null check | Add `if (value is null) continue;` guard |
| 11 | `SanitizeStringsFilter.cs` | 24-43 | Performance | Medium | Unbounded recursion on nested objects — malicious deep object could cause `StackOverflowException` | Add a `maxDepth` parameter with a hard cap (e.g. 5) |
| 12 | `SanitizeStringsFilter.cs` | 26-43 | Performance | Medium | `GetProperties()` via reflection on every request — no caching | Cache `PropertyInfo[]` in a static `ConcurrentDictionary<Type, PropertyInfo[]>` |
| 13 | `AppDbContext.cs` | 18 | Configuration | Medium | In-memory DB name is hardcoded (`"TodoList"`) — all instances share the same in-memory store | Make the DB name configurable via options or use a GUID in tests |
| 14 | `AppDbContext.cs` | — | Architecture | Low | No `OnModelCreating` — all constraints rely solely on data annotations | Add explicit index/constraint configuration in `OnModelCreating` |
| 15 | `Program.cs` | 21-22 | Security | Medium | `MapScalarApiReference()` is always enabled — exposes full API docs/testing UI in production | Wrap in `if (app.Environment.IsDevelopment())` |
| 16 | `Program.cs` | — | Security | High | No explicit CORS policy configured; `AllowedHosts: "*"` in appsettings | Add a CORS policy with `WithOrigins(...)` limiting to known frontend origins |
| 17 | `Program.cs` | — | Error Handling | High | No global exception handler — unhandled exceptions return raw 500s that may leak internals | Add `app.UseExceptionHandler(...)` or `app.UseProblemDetails()` |
| 18 | `Program.cs` | 24 | Security | Medium | `UseHttpsRedirection()` unconditionally enabled — can break HTTP-only dev/test flows | Apply only when `!app.Environment.IsDevelopment()` |
| 19 | `todoService.ts` | 33-39 | Error Handling | Medium | No request timeout — server stall will hang the browser indefinitely | Use `AbortController` with `setTimeout` (e.g. 10s) |
| 20 | `todoService.ts` | 47-66 | Security | Medium | `response.json()` called without verifying `Content-Type` is `application/json` | Check `response.headers.get('content-type')` before parsing |
| 21 | `todoService.ts` | 19-66 | Code Quality | Low | `response.json()` returns `any` — no runtime schema validation | Validate with Zod or io-ts to catch unexpected API shapes |
| 22 | `todoService.ts` | 27-30 | Security | Low | HTTP status codes exposed in user-facing error messages — leaks API implementation details | Show generic user messages; log details separately |
| 23 | `App.vue` | 77-93 | Bug | High | `finishedAt` set optimistically before API completes; rapid double-clicks cause inconsistent UI state | Update UI state only after API confirms success |
| 24 | `App.vue` | 49-75 | Code Quality | Low | `saving` ref exists but form submit button isn't disabled during in-flight requests — double-submits possible | Bind `saving` to `:disabled` on the ToDoForm submit button |
| 25 | `App.vue` | 17-103 | Bug | Medium | No coordination between concurrent async ops (create + delete can race) | Implement a request queue or global loading lock |
| 26 | `App.vue` | 53-60 | Code Quality | Low | `updatedAt` from API response is ignored after edit — displayed timestamp is stale | Assign server-returned timestamps back to local todo object |
| 27 | `ToDoForm.vue` | 38-59 | UX | Low | No `maxlength` on inputs — users can exceed 200/2000 char limits and only get feedback on submit | Add `maxlength="200"` / `maxlength="2000"` attributes |
| 28 | `ToDoForm.vue` | 23-30 | Code Quality | Low | Title validated with `.trim()` but submitted untrimmed — leading/trailing whitespace persists | `emit('save', { title: title.value.trim(), ... })` |
| 29 | `ToDoItem.vue` | 17-24 | i18n | Low | `'en-US'` locale hardcoded in date formatter | Use `navigator.language` or make locale injectable |
| 30 | `ToDoItem.vue` | 34 | Code Quality | Low | `$event.target as HTMLInputElement` cast without type guard — fragile if template changes | Use optional chaining: `($event.target as HTMLInputElement)?.checked` |
| 31 | `todo.ts` | 6-7 | Code Quality | Low | `createdAt`/`updatedAt` typed as `string`; `finishedAt` is `string \| null` — inconsistent | Use a branded type `ISODateTime = string & { __brand: 'ISO' }` or `Date` consistently |
| 32 | `App.spec.ts` | 16-52 | Testing | Medium | Tests cover only happy-path rendering — no error states, CRUD ops, or network failure paths | Add tests for error handling and each CRUD operation |
| 33 | `ToDoControllerTests.cs` | 14-178 | Testing | High | No integration tests — `SanitizeStringsFilter`, model validation, and AutoMapper are untested | Add integration tests using `WebApplicationFactory` |
| 34 | `ToDoControllerTests.cs` | 14-178 | Testing | Medium | No edge-case tests: empty strings, oversized inputs, XSS payloads | Add boundary tests and sanitization verification |
| 35 | `ToDoRepositoryTests.cs` | 13-201 | Testing | High | No tests for `SaveChangesAsync` failure, transaction rollback, or concurrent modification | Add failure-path and concurrency tests |
| 36 | `appsettings.json` | — | Security | Medium | No HSTS, CSP, or security header configuration | Configure security headers in `Program.cs` via `app.UseHsts()`, etc. |
| 37 | `vite.config.ts` | 22-26 | Configuration | Medium | Backend proxy URL `127.0.0.1:5128` is hardcoded | Use `process.env.VITE_API_TARGET ?? 'http://127.0.0.1:5128'` |
| 38 | `frontend/.env` | — | Configuration | Low | `.env` committed to version control — bad precedent even without secrets | Add `.env` to `.gitignore`; use `.env.example` as the only committed template |

---

## Summary by Severity

| Severity | Count |
|----------|-------|
| High | 8 |
| Medium | 16 |
| Low | 14 |

## Summary by Category

| Category | Count |
|----------|-------|
| Code Quality | 9 |
| Error Handling | 8 |
| Security | 6 |
| Testing | 5 |
| Performance | 4 |
| Configuration | 3 |
| Bug | 2 |
| UX / i18n | 2 |

---

## Top Priorities

1. **Global exception handler** (#17) — raw 500s can leak stack traces to clients
2. **CORS policy** (#16) — currently open to any origin
3. **Transaction error handling** (#3, #4) — undefined state on DB failure leaves data integrity at risk
4. **Optimistic update race condition** (#23) — visible bug reproducible with rapid UI interaction
5. **Integration tests** (#33) — filter and mapping code is completely unverified by the test suite
