# N2N User Server (n2n-user-server)

[![Language](https://img.shields.io/badge/Language-English-blue)](README.md) [![文档](https://img.shields.io/badge/文档-中文-red)](README.zh-CN.md)

An ASP.NET Core **user / administration server for an N2N VPN deployment**. It exposes an HTTPS API and a built-in web admin console to manage administrator accounts, edge users, and per-user N2N edge configurations (supernode address, community, encryption, etc.).

## ⚠️ Important: this project is entirely a shell

**This project does not implement any part of the N2N VPN protocol itself.** It is a pure management / shell layer (套壳): all actual VPN functionality — the supernode, the edge nodes, and the tunneling — is provided by [N2N](https://github.com/ntop/n2n) and the components listed below. This server only stores user data, issues session tokens, and hands edge configurations out to clients.

## Dependencies

> **This project cannot run standalone.** It is one part of a larger system and is designed to work together with the components below.

### [supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) — the foundation

[supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) is the base of everything: it is the **N2N supernode server and its web management tool** (dashboard / admin panel for an n2n supernode, built on the ntop n2n project). The user accounts and edge configurations managed by this server are meant to be consumed by a supernode deployment of this kind.

> **Note:** other N2N management tools may be used as well. This server only manages users and their edge configurations; any supernode / management tool that accepts the same n2n edge parameters (supernode IP & port, community name, device name, password, community key, encryption algorithm) can work with it.

### [n2n-winui](https://github.com/egoistic-eLily/n2n-winui) — the desktop GUI shell

[n2n-winui](https://github.com/egoistic-eLily/n2n-winui) is a **WinUI GUI shell written specifically for this project** (the repository is not yet public). It packages this server and the related tools into a desktop application for end users.

## Features

- **HTTPS API** — Kestrel serves all endpoints over HTTPS with a configurable PFX certificate.
- **Admin console built in** — the server statically hosts its own management web UI from `wwwroot/` (login, dashboard, user management, per-user N2N edge configuration management). No separate front-end deployment is needed.
- **Token-based sessions** — admin sessions use in-memory tokens with an expiry, issued after login and verified on every protected route; expired sessions are redirected to a timeout page.
- **Embedded database** — user accounts and N2N edge configurations are stored in a [LiteDB](https://www.litedb.org/) file; admin password hashing uses BCrypt.
- **Zero-config first run** — on first start the server creates a default `Config/config.json`, a `Cert/` folder, and a `Config/userdb.db` database next to the executable.

## Project structure

```
├── MAIN/                # Entry point (Program.cs)
├── Bootstrap/           # Startup: config loading, certificate/DB init, HTTP routing
├── API/                 # HTTP endpoints (login, page delivery, user & config CRUD)
├── Core/                # Business logic: token management, database operations
├── Services/            # Cross-cutting concerns: logging / error reporting
├── wwwroot/             # Built-in web admin console (HTML/CSS/JS, no build step)
├── libs/                # Local libraries: N2nKeygen (n2n user public key derivation, see libs/README.md)
├── thirdparty/          # Third-party sources (n2n, git submodule, GPLv3 — see thirdparty/README.md)
├── docs/                # Documentation (development manual, API reference)
└── Properties/          # Launch settings & publish profiles
```

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (the project targets `net10.0-windows`)
- **A standalone domain name** — the server only serves HTTPS traffic for a specific domain. **No default domain is provided**: set your own domain in the `url` field of `Config/config.json` and bind the certificate to it.
- **An SSL/TLS certificate in PFX format** — the server loads a PFX file at startup; convert your certificate (e.g. a Let's Encrypt `fullchain.pem` + `privkey.pem`) with:
  ```bash
  openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in fullchain.pem
  ```
- An N2N deployment as described in [Dependencies](#dependencies)

### Run

```bash
dotnet run
```

On first start the server creates a default configuration at `<output>/Config/config.json`. Adjust it before going to production:

| Field | Meaning |
| --- | --- |
| `certmode` | `Default` — look for `Cert/<url>.pfx` next to the executable, where `<url>` is the configured `url` field; `Manual` — use `pfxpath` |
| `pfxpath` / `pfxpassword` | Certificate path / password (used when `certmode` is `Manual`; empty by default) |
| `port` | HTTPS listening port |
| `userdbmode` / `userdbpath` | `Default` — use `Config/userdb.db`; `Manual` — use the given path |
| `admin_username` / `admin_password` | Initial console administrator credentials — **change them** |

> There is **no default domain, certificate, or certificate password**. Both the domain (`url`) and the certificate must be prepared by you; on first start the server writes them empty and refuses to start (with a clear log message) until they are configured.

Then open `https://<host>:<port>/admin_login` and sign in with the configured administrator account.

### Publish

```bash
dotnet publish -c Release
```

The `wwwroot/` front-end and the database/certificate folders are all resolved relative to the published executable.

## Documentation

- [Development manual](docs/DEVELOPMENT.md) — architecture, coding conventions, how to add endpoints and pages, build & publish.
- [API reference](docs/API.md) — every HTTP route and the request/response contract.
- [中文文档](README.zh-CN.md) — 中文版说明、开发手册与 API 文档。

## License

This repository uses layered licensing:

- **Server code** (everything except `libs/N2nKeygen/`): [MIT License](LICENSE).
- **`libs/N2nKeygen/`**: **GPLv3** (see its [LICENSE](libs/N2nKeygen/LICENSE)) — it is a
  cross-language port of n2n's GPLv3 key derivation sources, and ports of GPL code
  are derivative works.
- **`thirdparty/n2n`**: upstream n2n, GPLv3, tracked as a git submodule.

Consequence: the server binary statically links `N2nKeygen.dll`, so **if you
distribute the server**, the combined binary must be conveyed under GPLv3
(with corresponding source availability). Self-hosting without distribution
imposes no obligations.

> **Note:** this server talks to the N2N supernode/edge binaries **as separate processes only** (no source or library linkage), which is why the MIT license applies here. The N2N software itself (including any modified builds of it used in a deployment) is licensed by its authors under **GPLv3** — redistributing modified n2n binaries carries the GPLv3 source-availability obligation for that project, but it does not affect the license of this repository.
