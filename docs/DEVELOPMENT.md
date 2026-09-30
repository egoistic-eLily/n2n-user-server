# Development Manual

[![Language](https://img.shields.io/badge/Language-English-blue)](DEVELOPMENT.md) [![文档](https://img.shields.io/badge/文档-中文-red)](DEVELOPMENT.zh-CN.md)

Back to the [English README](../README.md) / [中文说明](../README.zh-CN.md).

This manual covers the architecture of the server, the conventions used in the codebase, and step-by-step recipes for the most common changes. For the HTTP contract see the [API reference](API.md).

## 1. Solution layout

The project is a single ASP.NET Core (`Microsoft.NET.Sdk.Web`) application targeting `net10.0-windows`. Code is organized by responsibility:

| Folder | Responsibility | Key files |
| --- | --- | --- |
| `MAIN/` | Process entry point | `Program.cs` — calls `Initialization.Init()` then `HttpBootstrap.Setup()` |
| `Bootstrap/` | Startup sequence | `Initialization.cs` (config / certificate / DB paths), `Config.cs` (config model), `HttpBootstrap.cs` (Kestrel + route table) |
| `API/` | HTTP endpoint handlers and their request/response DTOs | `LoginApi.cs` |
| `Core/` | Business logic | `Token.cs` (in-memory token store), `Database_Oper.cs` (LiteDB access, user & config operations) |
| `Services/` | Cross-cutting concerns | `ExceptionHandling.cs` (`ErrorReporter.Report(...)`) |
| `wwwroot/` | Built-in web admin console (static front-end) | `*.html`, `css/`, `js/` |
| `docs/` | Documentation | this manual, [API.md](API.md) |

### Startup flow

1. `Program.Main` → `Initialization.Init()`:
   - loads `Config/config.json` from the base directory (created with defaults on first run, enum values as strings);
   - resolves the TLS certificate — in `Default` mode it looks for `Cert/<url>.pfx` where `<url>` comes from the `url` field of the configuration (there is **no default domain**; it must be configured, otherwise startup is fatal); in `Manual` mode it uses `pfxpath`. The server is HTTPS-only and needs a **standalone domain**: the certificate must be a PFX issued for (and bound to) your own domain, converted e.g. with `openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in fullchain.pem`;
   - resolves the LiteDB path (`Config/userdb.db` in `Default` mode).
2. `HttpBootstrap.Setup()`:
   - builds Kestrel listening on `config.port` over HTTPS;
   - wires `UseDefaultFiles()` + `UseStaticFiles()` (serves `wwwroot/`) and the route table (`HttpBootstrap.ConfigureRoute`);
   - runs the app.

### Runtime artifacts (next to the executable, git-ignored)

| Path | Purpose |
| --- | --- |
| `Config/config.json` | Server configuration (see the [README](../README.md#getting-started)) |
| `Config/userdb.db` | LiteDB database: `users` and `n2n_configs` collections |
| `Cert/` | PFX certificate folder (`Default` cert mode) |
| `Logs/` | Serilog daily rolling log files (`server-YYYYMMDD.log`) |

## 2. Backend conventions

### Layering

- **API layer** (`API/`) only: parse the request body, verify the token, call into `Core`, shape the response. No business rules and no direct LiteDB access here beyond what `DatabaseOper` exposes.
- **Core layer** (`Core/`) owns state: the token dictionary (`Tokens`) and all database access (`DatabaseOper`). Handlers must go through `DatabaseOper` methods rather than touching collections directly.
- **Services layer** (`Services/`) is the single logging exit: `ErrorReporter.Report(...)`. It writes to Serilog, configured in `Initialization.InitLogger()` — console plus a daily rolling file under `Logs/` next to the executable. `Console.WriteLine` is **banned**; never log secrets (passwords, token values) either.
  - `Report(Services.LogLevel lv, string described)` — plain business log (request hits, successful operations).
  - `Report(Services.ExceptionType ex, Services.LogLevel lv, string described)` — log tagged with a source category (`DataBase`, `Http`, ...). `Fatal` flushes the log and terminates the process.

### Adding an HTTP endpoint

1. Define (or reuse) a request DTO and a response DTO next to the handler in `API/LoginApi.cs` — DTO property names are the JSON contract, keep them lowercase.
2. Write the handler as `public static async Task<IResult> X(HttpContext context)`:
   - `await context.Request.ReadFromJsonAsync<T>()`, return `Results.BadRequest(...)` when null;
   - call the shared `TokenVerify(...)` helper for protected routes (it already answers `recode 2000` on token timeout — the front-end depends on that value);
   - call `Core.DatabaseOper` methods and return `Results.Json(...)` with an explicit status code.
3. Register the route in `HttpBootstrap.ConfigureRoute` with a `MapPost`/`MapGet` and a comment.
4. Document the route in [API.md](API.md) and [API.zh-CN.md](API.zh-CN.md).

### Tokens

`Tokens.ObtainToken(TokenType, expireMinutes)` currently clears the whole token dictionary before issuing (single-admin assumption — see the comment in `Token.cs`). If you ever need multiple concurrent sessions, redesign `ObtainToken` and `VerifyToken` first.

### Database

All persistence goes through `DatabaseOper` / `DatabaseService` (LiteDB). `User` (accounts) and `UserData` (per-user n2n edge config) are the two document models, keyed by `BsonId`. Remember to add an index in `DatabaseService`'s constructor when introducing a new lookup field.

## 3. Front-end (`wwwroot/`) conventions

The admin console is plain HTML/CSS/vanilla JS — there is no build step. Each page is an `.html` file at the `wwwroot/` root, paired with a page-specific CSS file and a page-specific JS file:

| Page | Route (served by) | Files |
| --- | --- | --- |
| Login | `/admin_login` → `API.LoginApi.Admin_login_Html` | `login.html`, `css/login*.css`, `js/login.js` |
| Dashboard | `/admin` → `Admin_Index` | `admin.html`, `css/admin*.css`, `js/admin.js` |
| Users | `/users` → `Admin_Users_Tools` | `users.html`, `css/users*.css`, `js/users.js` |
| Configs | `/configs` → `Admin_Config_Tools` | `configs.html`, `css/configs*.css`, `js/configs.js` |
| Timeout landing | `/timeout.html` (static) | `timeout.html` |

HTML pages are read from `wwwroot` at runtime by `API.LoginApi.GetFilePath`, so a plain `dotnet publish` ships them — nothing to build or bundle.

### Page skeleton

Every admin page except the login page uses the same four regions: `app-sidebar`, `app-header`, `app-main`, `app-footer`. Navigation links carry `data-admin-nav`, and the current page marks itself with `aria-current="page"`. The account menu must use the `AdminButton`, `AdminMenu` and `LogoutButton` identifiers so `js/common.js` can bind their behavior automatically.

### Shared dialogs

Every admin page must include the `ErrorDialog` and `SuccessDialog` elements. Close buttons carry `data-dialog-close="dialog id"`; the common script binds them automatically. The error dialog uses `ErrorCode` and `ErrorHint`, the success dialog uses `SuccessHint`.

### API calls from JS

- All requests go through `AdminApp.post(route, payload, defaultHint)`.
- Responses are processed through `AdminApp.handleApiResult()`: it checks `recode === 2000` first (clears the token, redirects to the timeout page), then the HTTP status and the `success` flag, and shows `recode`/`hint` in the error dialog on failure.
- New protected endpoints should send the `sessionStorage` token in the request body and must be documented in [API.md](API.md) / [API.zh-CN.md](API.zh-CN.md).

### Styling rules

- Reusable rules shared by several admin pages go in `css/common.css`; page-specific styles only go in that page's own CSS file.
- Animation rules live in the separate `*.animations.css` files.
- Table rendering, filtering, pagination and form validation stay in the page's own JS file; anything shared belongs in `js/common.js`.

## 4. Build, run, publish

```bash
dotnet build            # compile
dotnet run              # run from source (creates Config/ and Cert/ on first run)
dotnet publish -c Release   # self-contained output in bin/Release/.../publish
```

`wwwroot/` is copied into the build/publish output by the explicit `Content Include="wwwroot\**"` rule in the `.csproj` (`API.LoginApi.GetFilePath` reads the files from disk at runtime, so the physical copy matters). `Config/` and `Cert/` are created next to the executable at runtime and are git-ignored.

## 5. Checklist for a new release

1. Default admin credentials in `Initialization.InitJson` — reviewed.
2. Routes registered in `HttpBootstrap.ConfigureRoute` — documented in [API.md](API.md) / [API.zh-CN.md](API.zh-CN.md).
3. Front-end pages affected? Update `css/`/`js/` per §3 and the page table above.
4. `dotnet publish -c Release` succeeds and `wwwroot/` is present in the output.
