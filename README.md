<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="src/Osiris.Web/wwwroot/images/osiris-logo-dark.svg" />
    <img src="src/Osiris.Web/wwwroot/images/osiris-logo-light.svg" alt="Osiris logo" width="120" />
  </picture>
</p>

<h1 align="center">Osiris</h1>

Osiris is the initial skeleton for a personal finance SaaS built with ASP.NET Core MVC, Identity, EF Core, PostgreSQL, MediatR, FluentValidation, Tailwind CSS, Alpine.js, HTMX, and Serilog.

This first stage contains authentication, tenant creation during registration, and a protected dashboard. It intentionally does not include finance CRUDs, payments, or tenant subdomain resolution yet.

## Financial Domain Model

The financial MVP follows the model documented in [docs/financial-model.md](docs/financial-model.md): credit card purchases are categorized expenses, statements group card debt, statement payments settle debt and account cash outflow without duplicating expenses, and bills are for off-card obligations.

## In-App User Documentation

Authenticated users can read end-user guides in the documentation area:

```text
/docs
```

Each guide has its own slug route, for example:

```text
/docs/categories
```

The pages render Markdown in the browser with CDN libraries, following the same documentation style used in the Contabil API reference page:

- `marked` parses Markdown.
- `DOMPurify` sanitizes the rendered HTML.
- `highlight.js` styles fenced code blocks if the guide later needs examples.

Documentation entries are registered in:

```text
src/Osiris.Web/Docs/catalog.json
```

Markdown sources are stored in the same folder, for example:

```text
src/Osiris.Web/Docs/categories.md
```

Protected Markdown endpoints follow the same slug:

```text
/docs/{slug}.md
```

To add documentation for another screen, add a Markdown file under `src/Osiris.Web/Docs`, register it in `catalog.json`, and link to `/docs/{slug}`. Keep end-user guides written for non-technical users. They should explain financial concepts in plain language and avoid accounting jargon.

## Prerequisites

- .NET SDK 10
- Docker
- Node.js 22+ and npm
- EF Core CLI 10.0.8

Update the EF CLI if needed:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.8
```

## Run The Local Stack

Start PostgreSQL and Seq:

```powershell
docker compose up -d
```

Development ports are standardized in the `13450-13500` range:

- PostgreSQL: `localhost:13450`
- Seq UI: `http://localhost:13451`
- Seq ingestion: `http://localhost:13452`
- Web app: `http://localhost:13453`

The local Seq container is configured without authentication for development.

The development connection string is in `src/Osiris.Web/appsettings.Development.json`:

```text
Host=localhost;Port=13450;Database=osiris_dev;Username=osiris;Password=osiris
```

## Apply Migrations

Create a migration:

```powershell
dotnet ef migrations add InitialCreate `
  --project src/Osiris.Infrastructure `
  --startup-project src/Osiris.Web `
  --output-dir Persistence/Migrations
```

Apply it:

```powershell
dotnet ef database update `
  --project src/Osiris.Infrastructure `
  --startup-project src/Osiris.Web
```

## Run The App

Quick Windows launcher:

```powershell
.\rodar-osiris.bat
```

The launcher frees port `13453`, starts Docker services, waits for PostgreSQL, applies migrations, and runs the Web project.

Manual run:

```powershell
dotnet run --project src/Osiris.Web
```

Useful routes:

- `http://localhost:13453/`
- `http://localhost:13453/account/register`
- `http://localhost:13453/account/login`
- `http://localhost:13453/account/forgotpassword`
- `http://localhost:13453/dashboard`

Registering a user creates a `Tenant`, creates an `ApplicationUser` linked by `TenantId`, signs the user in, and redirects to `/dashboard`.

## Tailwind CSS

The Web project uses Tailwind CSS v4 with a CSS-first setup.

Build CSS once:

```powershell
cd src/Osiris.Web
npm install
npm run copy:assets
npm run css:build
```

Watch during development:

```powershell
cd src/Osiris.Web
npm run css:watch
```

`dotnet build` also runs `npm install` if `node_modules` is missing, copies Alpine.js and HTMX to `wwwroot/lib`, and builds `wwwroot/css/app.css`.

## Tests

The solution includes unit and integration test projects from the start:

```text
tests/
  Osiris.Application.UnitTests/
  Osiris.Web.IntegrationTests/
```

Run all tests:

```powershell
dotnet test Osiris.sln
```

Run only unit tests:

```powershell
dotnet test tests/Osiris.Application.UnitTests
```

Run only integration tests:

```powershell
dotnet test tests/Osiris.Web.IntegrationTests
```

Integration tests use `WebApplicationFactory` and PostgreSQL through Testcontainers. Docker must be running for those tests.

Current coverage includes:

- FluentValidation rules for registration and login commands.
- Register user command handler behavior.
- Initial dashboard query data.
- Anonymous dashboard authorization redirect.
- Registration flow creating a tenant, creating an Identity user, signing in, and accessing `/dashboard`.

## Folder Structure

```text
src/
  Osiris.Domain/
  Osiris.Application/
  Osiris.Infrastructure/
  Osiris.Web/
tests/
  Osiris.Application.UnitTests/
  Osiris.Web.IntegrationTests/
```

- `Osiris.Domain`: domain entities with no framework dependencies.
- `Osiris.Application`: CQRS commands/queries, handlers, validation, pipeline behaviors, and service interfaces.
- `Osiris.Infrastructure`: EF Core, PostgreSQL, Identity, email placeholder, and service implementations.
- `Osiris.Web`: MVC controllers, Razor views, Tailwind assets, and startup configuration.
- `Osiris.Application.UnitTests`: isolated tests for Application validators, handlers, commands, and queries.
- `Osiris.Web.IntegrationTests`: MVC and persistence tests using a real PostgreSQL container.

## Next Steps

- Add real email delivery for password recovery.
- Add tenant resolution by subdomain or custom domain.
- Add roles and authorization policies.
- Add automated tests around registration, login, tenant creation, and dashboard access.
