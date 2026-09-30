# API Reference

[![Language](https://img.shields.io/badge/Language-English-blue)](API.md) [![文档](https://img.shields.io/badge/文档-中文-red)](API.zh-CN.md)

Back to the [English README](../README.md) / [中文说明](../README.zh-CN.md).

## General conventions

- All endpoints use `POST` with `Content-Type: application/json`.
- For authenticated admin endpoints, the token is read from `sessionStorage.getItem("token")` and sent as the `token` field of the request body.
- Business responses contain at least `success`, `recode` and `hint`. `recode` is a business return code and is not the HTTP status code.
- The front-end processes all admin responses through `AdminApp.post()` and `AdminApp.handleApiResult()`: it first checks `recode === 2000` (clears the token and redirects to `/timeout.html`), then the HTTP status and `success`; on failure it shows `recode` and `hint` in the default error dialog.

## Login

### `POST /api/login`

Request:

```json
{ "userid": "admin username", "password": "password" }
```

The success response additionally carries `token`. The front-end stores the token, shows a success dialog and redirects to `/admin?token=<token>` after confirmation.

## User management

### `POST /api/getuserdata`

Request: `{ "RequestType": "requestuser_data", "token": "..." }`.

The success response additionally carries a `usersdata` array; each item uses `id`, `username`, `enabled`.

### `POST /api/create_user`

Request: `{ "token": "...", "username": "...", "password": "...", "enabled": true }`.

A placeholder edge configuration is created automatically for the new user.

### `POST /api/revise_user`

Request: `{ "id": 1, "username": "...", "password_hash": "...", "enabled": true, "token": "..." }`.

The `password_hash` field carries the **plaintext** password (the backend hashes it); the literal string `"null"` means "keep the current password".

### `POST /api/delete_user`

Request: `{ "id": 1, "username": "...", "password_hash": "", "enabled": true, "token": "..." }`.

Deletes the user and their edge configuration.

## User edge configuration management

### `POST /api/getusersconfig`

Request: `{ "RequestType": "requestuser_config", "token": "..." }`.

The success response additionally carries a `usersconfig` array; each item uses `user_id`, `supernode_ip`, `supernode_port`, `community_name`, `device_name`, `encrypt_algorithm`.

### `POST /api/create_config` and `POST /api/revise_config`

Both take the fields `token`, `user_id`, `supernode_ip`, `supernode_port`, `community_name`, `device_name`, `password`, `community_key`, `encrypt_algorithm`. In the revise form, `user_id` is read-only.

## Logout

### `POST /api/logout`

Request: `{ "RequestType": "logout", "token": "..." }`.

On success the front-end clears the `sessionStorage` token, shows a success dialog and redirects to `/admin_login` when the dialog is closed.

## Error responses

All protected endpoints share the same error shape:

```json
{ "success": false, "recode": 401, "hint": "token error" }
```

| `recode` | Meaning | Front-end behavior |
| --- | --- | --- |
| `2000` | Token expired (`TimeOut`) | Clear token, redirect to `/timeout.html` |
| `401` | Token missing / invalid | Show error dialog |
| other | Operation-specific failure | Show `recode` + `hint` in error dialog |
