# N2N User Server（n2n-user-server）

[![Language](https://img.shields.io/badge/Language-English-blue)](README.md) [![文档](https://img.shields.io/badge/文档-中文-red)](README.zh-CN.md)

一个面向 N2N VPN 部署的 **用户 / 管理服务器**，基于 ASP.NET Core 构建。它通过 HTTPS 提供 API，并内置一个 Web 管理控制台，用于管理管理员账号、边缘（edge）用户，以及每个用户的 N2N 边缘节点配置（超级节点地址、社区、加密方式等）。

## ⚠️ 重要声明：本项目完完全全是一个套壳

**本项目没有实现 N2N VPN 协议的任何部分。** 它是一个纯粹的管理层 / 套壳程序：所有真正的 VPN 功能——超级节点（supernode）、边缘节点（edge）、隧道传输——全部由 [N2N](https://github.com/ntop/n2n) 及下文列出的组件提供。本服务器只负责存储用户数据、签发会话令牌（token）、向客户端下发边缘节点配置。

## 依赖项目

> **本项目无法独立运行。** 它是一个完整系统的一部分，需要与下列组件配合使用。

### [supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) —— 一切的基础

[supernode-frontend](https://github.com/ChingCdesu/supernode-frontend) 是本项目的基础：它是 **N2N 超级节点服务器及其管理工具**（基于 ntop 的 n2n 项目构建的超级节点仪表盘 / 管理面板）。本服务器管理的用户账号与边缘配置，正是提供给这类超级节点部署使用的。

> **说明：** 也可以使用其他的 N2N 管理工具。本服务器只负责管理用户及其边缘配置，任何接受相同 n2n 边缘参数（超级节点 IP 与端口、社区名、设备名、密码、社区密钥、加密算法）的超级节点 / 管理工具均可与本项目配合使用。

### [N2N_TOOLS_WINUI](https://github.com/egoistic-eLily/N2N_TOOLS_WINUI) —— 桌面 GUI 套壳程序

[N2N_TOOLS_WINUI](https://github.com/egoistic-eLily/N2N_TOOLS_WINUI) 是**专门为本项目编写的 WinUI GUI 套壳程序**（该仓库暂未公开）。它将本服务器及相关工具打包为一个面向最终用户的桌面应用程序。

## 功能特性

- **HTTPS API** —— Kestrel 使用可配置的 PFX 证书，通过 HTTPS 提供所有接口。
- **内置管理控制台** —— 服务器直接从 `wwwroot/` 托管自带的管理网页（登录页、控制台主页、用户管理、用户 N2N 边缘配置管理），无需单独部署前端。
- **基于 Token 的会话** —— 管理会话使用带过期时间的内存 Token，登录后签发，每个受保护路由都会校验；会话过期会跳转到超时页面。
- **嵌入式数据库** —— 用户账号与 N2N 边缘配置存储在 [LiteDB](https://www.litedb.org/) 数据库文件中；管理员密码使用 BCrypt 哈希。
- **首次启动零配置** —— 首次启动时，服务器会在可执行文件旁自动生成默认的 `Config/config.json`、`Cert/` 目录和 `Config/userdb.db` 数据库。

## 项目结构

```
├── MAIN/                # 程序入口（Program.cs）
├── Bootstrap/           # 启动流程：配置加载、证书/数据库初始化、HTTP 路由
├── API/                 # HTTP 接口（登录、页面分发、用户与配置增删改查）
├── Core/                # 业务逻辑：Token 管理、数据库操作
├── Services/            # 横切关注点：日志 / 错误上报
├── wwwroot/             # 内置 Web 管理控制台（HTML/CSS/JS，无需构建）
├── docs/                # 文档（开发手册、API 参考）
└── Properties/          # 启动设置与发布配置
```

## 快速开始

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/)（项目目标框架为 `net10.0-windows`）
- **一个独立域名** —— 服务器只针对特定域名提供 HTTPS 服务。**本项目不提供任何默认域名**：需要你在 `Config/config.json` 的 `url` 字段中配置自己的域名，并将证书绑定到该域名
- **一份 SSL/TLS 证书，并转换为 PFX 格式** —— 服务器启动时加载的是 PFX 文件；可用如下命令将证书（例如 Let's Encrypt 的 `fullchain.pem` + `privkey.pem`）转换为 PFX：
  ```bash
  openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in fullchain.pem
  ```
- 按 [依赖项目](#依赖项目) 所述部署好的 N2N 环境

### 运行

```bash
dotnet run
```

首次启动时，服务器会在 `<输出目录>/Config/config.json` 生成默认配置，正式使用前请调整：

| 字段 | 含义 |
| --- | --- |
| `certmode` | `Default` —— 在可执行文件旁寻找 `Cert/<url>.pfx`，`<url>` 为配置中的 `url` 字段；`Manual` —— 使用 `pfxpath` 指定的证书 |
| `pfxpath` / `pfxpassword` | 证书路径 / 密码（`certmode` 为 `Manual` 时生效；默认为空） |
| `port` | HTTPS 监听端口 |
| `userdbmode` / `userdbpath` | `Default` —— 使用 `Config/userdb.db`；`Manual` —— 使用指定路径 |
| `admin_username` / `admin_password` | 控制台初始管理员凭据 —— **请务必修改** |

> 本项目**不提供任何默认域名、默认证书或默认证书密码**。域名（`url`）与证书均需自行准备；首次启动时这些字段为空，未配置完成前服务器会拒绝启动（日志中会给出明确提示）。

然后打开 `https://<主机>:<端口>/admin_login`，使用配置好的管理员账号登录。

### 发布

```bash
dotnet publish -c Release
```

`wwwroot/` 前端以及数据库 / 证书目录均相对于发布后的可执行文件解析。

## 文档

- [开发手册](docs/DEVELOPMENT.zh-CN.md) —— 架构说明、编码规范、如何新增接口与页面、构建与发布。
- [API 参考](docs/API.zh-CN.md) —— 全部 HTTP 路由及请求 / 响应约定。
- [English documentation](README.md) — 英文版说明、开发手册与 API 文档。

## 许可证

本项目基于 [MIT License](LICENSE) 开源。

> **说明：** 本服务器与 N2N 超级节点 / 边缘节点程序之间**仅为进程级依赖**（不涉及源码或库链接），因此本项目可使用 MIT 协议。N2N 软件本身（包括部署中使用的任何二次编译版本）由其作者以 **GPLv3** 授权——再分发修改版 n2n 二进制时，需按 GPLv3 公开该项目的源码，但这不影响本仓库的 MIT 协议。
