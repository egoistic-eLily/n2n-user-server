# 开发手册

[![Language](https://img.shields.io/badge/Language-English-blue)](DEVELOPMENT.md) [![文档](https://img.shields.io/badge/文档-中文-red)](DEVELOPMENT.zh-CN.md)

返回 [中文说明](../README.zh-CN.md) / [English README](../README.md)。

本手册介绍服务器的架构、代码库中使用的规范，以及常见改动的操作步骤。HTTP 契约详见 [API 参考](API.zh-CN.md)。

## 1. 解决方案布局

项目是单个 ASP.NET Core（`Microsoft.NET.Sdk.Web`）应用程序，目标框架 `net10.0-windows`。代码按职责分层：

| 目录 | 职责 | 关键文件 |
| --- | --- | --- |
| `MAIN/` | 进程入口 | `Program.cs` —— 依次调用 `Initialization.Init()` 与 `HttpBootstrap.Setup()` |
| `Bootstrap/` | 启动流程 | `Initialization.cs`（配置 / 证书 / 数据库路径）、`Config.cs`（配置模型）、`HttpBootstrap.cs`（Kestrel + 路由表） |
| `API/` | HTTP 接口处理函数及请求 / 响应 DTO | `LoginApi.cs` |
| `Core/` | 业务逻辑 | `Token.cs`（内存 Token 存储）、`Database_Oper.cs`（LiteDB 访问、用户与配置操作） |
| `Services/` | 横切关注点 | `ExceptionHandling.cs`（`ErrorReporter.Report(...)`） |
| `wwwroot/` | 内置 Web 管理控制台（静态前端） | `*.html`、`css/`、`js/` |
| `docs/` | 文档 | 本手册、[API.zh-CN.md](API.zh-CN.md) |

### 启动流程

1. `Program.Main` → `Initialization.Init()`：
   - 从基目录加载 `Config/config.json`（首次运行自动生成默认配置，枚举以字符串形式序列化）；
   - 解析 TLS 证书 —— `Default` 模式在 `Cert/` 目录下寻找 `Cert/<url>.pfx`，`<url>` 来自配置中的 `url` 字段（**不提供默认域名**，必须自行配置，否则视为致命错误）；`Manual` 模式使用 `pfxpath`。服务器只提供 HTTPS 服务，且需要一个**独立域名**：证书必须是为你自己域名签发（并绑定）的 PFX 文件，可用 `openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in fullchain.pem` 转换；
   - 解析 LiteDB 路径（`Default` 模式为 `Config/userdb.db`）。
2. `HttpBootstrap.Setup()`：
   - 构建 Kestrel，监听 `config.port`，走 HTTPS；
   - 注册 `UseDefaultFiles()` + `UseStaticFiles()`（托管 `wwwroot/`）以及路由表（`HttpBootstrap.ConfigureRoute`）；
   - 启动应用。

### 运行时产物（位于可执行文件旁，已被 git 忽略）

| 路径 | 用途 |
| --- | --- |
| `Config/config.json` | 服务器配置（见 [README](../README.zh-CN.md#快速开始)） |
| `Config/userdb.db` | LiteDB 数据库：`users` 与 `n2n_configs` 两个集合 |
| `Cert/` | PFX 证书目录（`Default` 证书模式） |
| `Logs/` | Serilog 按天滚动的日志文件（`server-YYYYMMDD.log`） |

## 2. 后端规范

### 分层

- **API 层**（`API/`）只做三件事：解析请求体、校验 Token、调用 `Core` 并组装响应。不允许在这里写业务规则，除 `DatabaseOper` 暴露的方法外不得直接访问 LiteDB。
- **Core 层**（`Core/`）持有全部状态：Token 字典（`Tokens`）与所有数据库访问（`DatabaseOper`）。处理函数必须经由 `DatabaseOper` 的方法，而不是直接操作集合。
- **Services 层**（`Services/`）是唯一的日志出口：`ErrorReporter.Report(...)`。它写入 Serilog，配置在 `Initialization.InitLogger()` 中——控制台 + 可执行文件旁 `Logs/` 目录下按天滚动的日志文件。**禁止使用 `Console.WriteLine`**，同样禁止记录敏感信息（密码、Token 明文）。
  - `Report(Services.LogLevel lv, string described)` —— 普通业务日志（请求到达、操作成功等）。
  - `Report(Services.ExceptionType ex, Services.LogLevel lv, string described)` —— 带来源类别的日志（`DataBase`、`Http` 等）。`Fatal` 会刷新日志并终止进程。

### 新增 HTTP 接口

1. 在 `API/LoginApi.cs` 中处理函数旁定义（或复用）请求 / 响应 DTO——DTO 属性名即 JSON 契约，请保持小写。
2. 处理函数写成 `public static async Task<IResult> X(HttpContext context)`：
   - `await context.Request.ReadFromJsonAsync<T>()`，为 null 时返回 `Results.BadRequest(...)`；
   - 受保护路由调用共用的 `TokenVerify(...)` 辅助函数（Token 超时它会返回 `recode 2000`——前端依赖该值，不要改动）；
   - 调用 `Core.DatabaseOper` 的方法，并以 `Results.Json(...)` 返回、显式指定状态码。
3. 在 `HttpBootstrap.ConfigureRoute` 中用 `MapPost`/`MapGet` 注册路由并附注释。
4. 在 [API.zh-CN.md](API.zh-CN.md) 与 [API.md](API.md) 中补文档。

### Token

`Tokens.ObtainToken(TokenType, expireMinutes)` 目前在签发前会清空整个 Token 字典（单管理员假设——见 `Token.cs` 内注释）。如果将来需要多会话并发，请先重新设计 `ObtainToken` 与 `VerifyToken`。

### 数据库

所有持久化都经由 `DatabaseOper` / `DatabaseService`（LiteDB）。`User`（账号）与 `UserData`（每用户的 n2n 边缘配置）是两个文档模型，以 `BsonId` 为主键。新增查询字段时，请在 `DatabaseService` 构造函数中补充索引。

## 3. 前端规范（`wwwroot/`）

管理控制台是纯 HTML/CSS/原生 JS——没有构建步骤。每个页面对应 `wwwroot/` 根目录下的一个 `.html` 文件，并配有该页专属的 CSS 与 JS 文件：

| 页面 | 路由（由后端提供） | 文件 |
| --- | --- | --- |
| 登录 | `/admin_login` → `API.LoginApi.Admin_login_Html` | `login.html`、`css/login*.css`、`js/login.js` |
| 控制台主页 | `/admin` → `Admin_Index` | `admin.html`、`css/admin*.css`、`js/admin.js` |
| 用户管理 | `/users` → `Admin_Users_Tools` | `users.html`、`css/users*.css`、`js/users.js` |
| 用户配置管理 | `/configs` → `Admin_Config_Tools` | `configs.html`、`css/configs*.css`、`js/configs.js` |
| 会话超时页 | `/timeout.html`（静态） | `timeout.html` |

HTML 页面在运行时由 `API.LoginApi.GetFilePath` 从 `wwwroot` 读取，因此直接 `dotnet publish` 即可携带它们——无需构建或打包。

### 页面骨架

除登录页外的每个管理页面都使用相同的四个区域：`app-sidebar`、`app-header`、`app-main`、`app-footer`。导航链接需加 `data-admin-nav`，当前页面使用 `aria-current="page"` 标记。账号菜单必须使用 `AdminButton`、`AdminMenu`、`LogoutButton` 标识，以便 `js/common.js` 自动绑定交互。

### 公共对话框

每个管理页面必须包含 `ErrorDialog` 与 `SuccessDialog` 元素。关闭按钮需加 `data-dialog-close="对话框 ID"`，公共脚本会自动绑定关闭事件。错误窗口使用 `ErrorCode` 与 `ErrorHint`，成功窗口使用 `SuccessHint`。

### JS 侧的 API 调用

- 所有请求统一经 `AdminApp.post(route, payload, defaultHint)` 发出。
- 响应统一经 `AdminApp.handleApiResult()` 处理：先检查 `recode === 2000`（清除 Token 并跳转超时页），再检查 HTTP 状态与 `success` 标志，失败时在错误窗口显示 `recode` 与 `hint`。
- 新增受保护接口应将 `sessionStorage` 中的 Token 写入请求体，并在 [API.zh-CN.md](API.zh-CN.md) / [API.md](API.md) 中补充字段说明。

### 样式规则

- 多个管理页面复用的规则放 `css/common.css`；页面专属样式只放该页自己的 CSS 文件。
- 动画规则放在独立的 `*.animations.css` 文件中。
- 表格渲染、筛选、分页与表单校验留在各页面自己的 JS 文件中；可共享的逻辑归入 `js/common.js`。

## 4. 构建、运行、发布

```bash
dotnet build            # 编译
dotnet run              # 源码运行（首次运行会创建 Config/ 与 Cert/）
dotnet publish -c Release   # 发布到 bin/Release/.../publish
```

`wwwroot/` 通过 `.csproj` 中显式的 `Content Include="wwwroot\**"` 规则复制进构建 / 发布输出（`API.LoginApi.GetFilePath` 在运行时直接从磁盘读取这些文件，因此物理复制是必需的）。`Config/` 与 `Cert/` 在运行时于可执行文件旁创建，且已被 git 忽略。

## 5. 发布前检查清单

1. `Initialization.InitJson` 中的默认管理员凭据 —— 已确认修改。
2. `HttpBootstrap.ConfigureRoute` 中注册的新路由 —— 已补充到 [API.zh-CN.md](API.zh-CN.md) / [API.md](API.md)。
3. 涉及前端页面？按第 3 节更新 `css/`/`js/` 并同步上表。
4. `dotnet publish -c Release` 成功，且输出中包含 `wwwroot/`。
