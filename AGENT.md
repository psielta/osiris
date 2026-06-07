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

## Git Commits

Use Conventional Commits for every app commit.

Do not add `Co-authored-by` trailers.
