# Agent Instructions

## Architecture

Keep the dependency direction:

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application -> Domain
```

`Domain` must not depend on ASP.NET Core, EF Core, Identity, MediatR, or Infrastructure.

`Application` owns commands, queries, handlers, validators, DTOs, pipeline behaviors, and interfaces.

`Infrastructure` owns EF Core, PostgreSQL, Identity, email implementations, and service implementations.

`Web` owns MVC controllers, Razor views, static assets, and startup wiring.

## Controllers

Controllers must stay thin:

- Do not inject `ApplicationDbContext`.
- Do not inject `UserManager` or `SignInManager`.
- Do not put business logic in controllers.
- Use `IMediator.Send(...)` for commands and queries.
- Convert validation/business errors into `ModelState` and return the same view.

## Commands And Queries

Add new behavior through CQRS:

```text
Features/<Area>/Commands/<UseCase>/<UseCase>Command.cs
Features/<Area>/Commands/<UseCase>/<UseCase>CommandValidator.cs
Features/<Area>/Commands/<UseCase>/<UseCase>CommandHandler.cs
Features/<Area>/Queries/<UseCase>/<UseCase>Query.cs
Features/<Area>/Queries/<UseCase>/<UseCase>QueryHandler.cs
```

All input validation belongs in FluentValidation validators.

Expected MediatR pipeline order:

```text
Request -> ValidationBehavior -> LoggingBehavior -> Handler
```

## Authentication

Authentication flows go through MediatR:

- `RegisterUserCommand`
- `LoginUserCommand`
- `LogoutUserCommand`
- `ForgotPasswordCommand`

Identity implementation details stay in `IIdentityService` implementations inside Infrastructure.

Do not log password reset tokens.

All auth POST actions must use antiforgery validation.

## Tailwind

Tailwind input lives at `src/Osiris.Web/Styles/app.css`.

Build CSS with:

```powershell
cd src/Osiris.Web
npm run css:build
```

Use `npm run css:watch` during UI work.

`dotnet build` should remain able to generate the CSS output.

## User-Facing Language

The app is for Brazilian users. All user-facing UI text must be written in Brazilian Portuguese.

This includes Razor views, layout text, buttons, links, form labels, placeholders, empty states, table headers, select options, validation messages, business-rule errors, success messages, and in-app documentation.

Code identifiers, routes, commands, queries, DTOs, tests, folders, database objects, and internal architecture terms may remain in English.

## Testing

Tests are part of the architecture, not an optional follow-up.

Keep test projects under `tests/`:

- `Osiris.Application.UnitTests`: unit tests for commands, queries, handlers, validators, DTO mapping, and Application behavior.
- `Osiris.Web.IntegrationTests`: integration tests for MVC routes, authentication flows, EF Core, Identity, PostgreSQL, and tenant creation.

Testing rules:

- Add or update tests whenever behavior changes.
- Prefer unit tests for Application handlers and FluentValidation validators.
- Prefer integration tests for controller routes, antiforgery flows, authentication, authorization, database persistence, and multitenancy behavior.
- Controllers must not be unit-tested by mocking business logic when a full MVC integration test is more valuable.
- Do not use EF Core in-memory provider for integration tests.
- Integration tests that touch persistence must use PostgreSQL through Testcontainers.
- Keep tests readable and focused on observable behavior.
- `dotnet test Osiris.sln` should pass before handing work back.

## Local Ports

Use the `13450-13500` range for this app's local development ports.

When adding future Docker services or locally exposed services, assign host ports inside that range and document them in `README.md`.

Current assignments:

- PostgreSQL: `13450`
- Seq UI: `13451`
- Seq ingestion: `13452`
- Web HTTP: `13453`
- Web HTTPS: `13454`

## Versioning

The Web app version is user-visible in the authenticated sidebar footer.

Keep these version values aligned:

- `src/Osiris.Web/Osiris.Web.csproj` (`<Version>`)
- `src/Osiris.Web/package.json` (`version`)
- `src/Osiris.Web/package-lock.json` root package version

Use SemVer-style increments:

- Patch: bug fixes, UI polish, copy changes, documentation-only app updates, and other backward-compatible maintenance.
- Minor: new user-facing features, new screens, new financial flows, or backward-compatible data model additions.
- Major: incompatible behavior, API, migration, or data-contract changes.

For pre-1.0 releases, still use patch/minor intentionally: patch for fixes and polish, minor for new usable capability. If a change is not intended to ship to users, do not bump the version just to commit internal work.

## Git Commits

Use Conventional Commits for every app commit.

Do not add `Co-authored-by` trailers.
