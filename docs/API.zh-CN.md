# API 参考（API Reference）

[![Language](https://img.shields.io/badge/Language-English-blue)](API.md) [![文档](https://img.shields.io/badge/文档-中文-red)](API.zh-CN.md)

返回 [中文说明](../README.zh-CN.md) / [English README](../README.md)。

## 通用约定

- 所有接口使用 `POST`，请求头为 `Content-Type: application/json`。
- 已登录管理接口的 token 从 `sessionStorage.getItem("token")` 获取，并作为请求体的 `token` 字段发送。
- 业务响应至少包含 `success`、`recode` 与 `hint`。`recode` 为业务返回码，不等同于 HTTP 状态码。
- 前端统一通过 `AdminApp.post()` 与 `AdminApp.handleApiResult()` 处理管理接口响应：先检查 `recode === 2000`，清理 token 并跳转 `/timeout.html`；随后检查 HTTP 状态与 `success`；失败时在默认错误窗口显示 `recode` 和 `hint`。

## 登录

### `POST /api/login`

请求：

```json
{ "userid": "管理员用户名", "password": "密码" }
```

成功响应额外提供 `token`。前端保存 token 后显示成功窗口，确认后跳转 `/admin?token=<token>`。

## 用户管理

### `POST /api/getuserdata`

请求：`{ "RequestType": "requestuser_data", "token": "..." }`。

成功响应额外提供 `usersdata` 数组；每项使用 `id`、`username`、`enabled`。

### `POST /api/create_user`

请求：`{ "token": "...", "username": "...", "password": "...", "enabled": true }`。

### `POST /api/revise_user`

请求：`{ "id": 1, "username": "...", "password_hash": "...", "enabled": true, "token": "..." }`。

`password_hash` 字段传入明文密码，由后端处理；字符"null"表示不修改密码。

### `POST /api/delete_user`

请求：`{ "id": 1, "username": "...", "password_hash": "", "enabled": true, "token": "..." }`。

## 用户配置管理

### `POST /api/getusersconfig`

请求：`{ "RequestType": "requestuser_config", "token": "..." }`。

成功响应额外提供 `usersconfig` 数组；每项使用 `user_id`、`supernode_ip`、`supernode_port`、`community_name`、`device_name`、`encrypt_algorithm`。

### `POST /api/create_config` 与 `POST /api/revise_config`

请求字段均为 `token`、`user_id`、`supernode_ip`、`supernode_port`、`community_name`、`device_name`、`password`、`community_key`、`encrypt_algorithm`。修改操作中的 `user_id` 由界面设为只读。

## 退出登录

### `POST /api/logout`

请求：`{ "RequestType": "logout", "token": "..." }`。

成功后清除 sessionStorage token，显示成功窗口；关闭窗口后跳转 `/admin_login`。

## 错误响应

所有受保护接口共用同一错误结构：

```json
{ "success": false, "recode": 401, "hint": "token error" }
```

| `recode` | 含义 | 前端行为 |
| --- | --- | --- |
| `2000` | Token 已过期（`TimeOut`） | 清除 token，跳转 `/timeout.html` |
| `401` | Token 缺失或无效 | 显示错误窗口 |
| 其他 | 具体操作失败 | 在错误窗口显示 `recode` + `hint` |
