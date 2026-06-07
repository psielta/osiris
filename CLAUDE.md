# Claude Instructions

Follow `AGENT.md` for architecture, validation, MediatR, Identity, and Tailwind rules.

Before implementing changes:

1. Check the current repo state.
2. Keep controllers thin.
3. Route business behavior through Application commands and queries.
4. Keep Infrastructure details out of Domain and Web controllers.
5. Add or update tests for changed behavior.
6. Run `dotnet build Osiris.sln` and `dotnet test Osiris.sln` before handing work back.

Testing rules:

- Unit tests belong in `tests/Osiris.Application.UnitTests`.
- Integration tests belong in `tests/Osiris.Web.IntegrationTests`.
- Use unit tests for validators, handlers, commands, and queries.
- Use integration tests for MVC routes, antiforgery, authentication, authorization, EF Core, Identity, PostgreSQL, and tenant behavior.
- Do not use EF Core in-memory provider for integration tests; use PostgreSQL through Testcontainers.

Use the `13450-13500` range for this app's local development ports. Future Docker services or local service bindings must expose host ports in that range and be documented in `README.md`.

When creating commits, always use Conventional Commits and do not add `Co-authored-by` trailers.
