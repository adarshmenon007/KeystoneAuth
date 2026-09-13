# KeystoneAuth

A standalone JWT authentication reference implementation: register / login / refresh / logout, with rotating, reuse-detecting refresh tokens, built as a Clean Architecture + CQRS (MediatR) demo in ASP.NET Core and Blazor WebAssembly.

This repo exists to demonstrate the auth pattern in isolation — it deliberately has **no business domain**. The one non-auth endpoint (`GET /me`) exists purely to prove a protected route validating the issued JWT end to end.

## What this demonstrates

- **Clean Architecture**, 6 projects, dependency-inward only: `Keystone.Domain` → `Keystone.Application` → `Keystone.Infrastructure` / `Keystone.Api` / `Keystone.Web`, with `Keystone.Contracts` as the wire-format layer shared between `Api` and `Web`.
- **CQRS via MediatR** — `RegisterUserCommand`, `LoginUserCommand`, `RefreshTokenCommand`, `LogoutCommand`, each a thin handler delegating to two Infrastructure seams (`IIdentityService`, `IAuthTokenService`).
- **ErrorOr** for expected failures (bad credentials, duplicate email, invalid refresh token) instead of exceptions, with named static error classes (`IdentityErrors`).
- **Rotating refresh tokens with reuse detection** — the core pattern this repo is built to showcase. Every refresh token belongs to a "family"; presenting an already-rotated token is treated as a stolen-token signal and revokes the entire family, not just that token. See `Keystone.Infrastructure/Identity/AuthTokenService.cs` and its integration tests.
- **Rate limiting, structured audit logging, and CORS** on every auth endpoint — closing the gaps a production auth service needs that a bare tutorial usually skips.
- **Blazor WebAssembly** front end with browser-token storage, a `DelegatingHandler`-based bearer/refresh-on-401 pipeline, and `AuthenticationStateProvider` integration.

## Architecture

```
Keystone.Web  ───HTTP───▶  Keystone.Api  ───▶  Keystone.Application  ───▶  Keystone.Domain
   (Blazor WASM)              (Controllers)         (MediatR handlers,       (AuthenticatedAccount)
        │                          │                  ErrorOr, seams)
        │                          ▼
        └──────uses──────▶  Keystone.Contracts   Keystone.Infrastructure
                             (request/response      (ASP.NET Core Identity,
                              records)                EF Core + SQLite, JWT)
```

Dependencies point inward only: `Api` wires up `Infrastructure` via one DI extension (`AddInfrastructure`) and never references its types directly; `Web` references only `Contracts` and talks to `Api` purely over HTTP.

## What's deliberately out of scope (and why)

- **No business domain.** This repo proves the auth pattern in isolation. A companion project (not linked to this one at runtime) explores CQRS/Clean Architecture applied to a business domain instead.
- **No roles or claims-based authorization** beyond "is this a valid, authenticated user" — adding role management would be new scope, not a faithful demo of the pattern this repo focuses on.
- **No email verification or password reset** — the password policy here is a deliberately minimal demo baseline (min length 8, default ASP.NET Identity lockout), not a production-complete policy.
- **No integration with any other repo** — this is a fully standalone, independently runnable project.

## Running it locally

Requires the .NET 10 SDK (pinned via `global.json`).

**1. Set the JWT signing key** (the API refuses to start without one ≥32 characters):

```bash
dotnet user-secrets set "Jwt:SigningKey" "some-random-string-at-least-32-characters" --project src/Keystone.Api
```

**2. Run the API:**

```bash
dotnet run --project src/Keystone.Api --launch-profile https
```

It listens on `https://localhost:7501` and creates `keystone.db` (SQLite) on first run — no external database needed.

**3. Run the Web app** (in a second terminal):

```bash
dotnet run --project src/Keystone.Web --launch-profile https
```

Opens a browser at `https://localhost:7601`. Register an account, then visit "Profile" to see a live, token-authenticated API call.

## Running the tests

```bash
dotnet test KeystoneAuth.slnx
```

Two test projects:
- `Keystone.Application.UnitTests` — command handlers against mocked seams (no database).
- `Keystone.Infrastructure.IntegrationTests` — the identity store and the refresh-token rotation/reuse-detection logic against a real (in-memory) SQLite database, since that behavior can't be trusted to mocks.

## License

MIT — see [LICENSE](LICENSE).
