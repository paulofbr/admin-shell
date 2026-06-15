# API Reference

This page documents the public endpoints exposed by the Admin Shell API.

---

## Base URL

```
http://localhost:5000
```

In production, replace with your deployed URL.

---

## Authentication

Most endpoints require a valid JWT token in the `Authorization` header:

```
Authorization: Bearer <token>
```

### POST /api/auth/login

Authenticates a user and returns a JWT token.

**Request Body:**

```json
{
  "email": "admin@shell.com",
  "password": "Admin123!"
}
```

**Response (200):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-06-08T12:00:00Z",
  "user": {
    "id": "guid",
    "email": "admin@shell.com",
    "name": "Admin User",
    "roles": ["admin"]
  }
}
```

---

## Health

### GET /api/health

Returns the health status of the application.

**Response (200):**

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "application",
      "status": "Healthy",
      "description": "Application is running"
    }
  ]
}
```

No authentication required.

---

## User Management

### GET /api/users

Retrieves a paginated list of users.

**Query Parameters:**

| Parameter  | Type    | Default | Description                |
|------------|---------|---------|----------------------------|
| `page`     | int     | `1`     | Page number                |
| `pageSize` | int     | `20`    | Items per page (max 100)   |
| `search`   | string  | —       | Search by name or email    |

**Response (200):**

```json
{
  "items": [
    {
      "id": "guid",
      "email": "admin@shell.com",
      "name": "Admin User",
      "roles": ["admin"],
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Required Permission:** `users:read`

### POST /api/users

Creates a new user.

**Request Body:**

```json
{
  "email": "newuser@shell.com",
  "password": "SecureP@ss1",
  "name": "New User",
  "roles": ["editor"]
}
```

**Response (201):** Returns the created user object.

**Required Permission:** `users:create`

### PUT /api/users/{id}

Updates an existing user.

**Required Permission:** `users:update`

### DELETE /api/users/{id}

Deletes a user (soft delete).

**Required Permission:** `users:delete`

---

## Plugin API Endpoints

Plugin endpoints follow the convention `/api/plugins/{pluginId}/...`.

### Reporting Plugin

All endpoints under `/api/plugins/reporting/`.

#### GET /api/plugins/reporting/reports

Returns a list of available reports.

```json
[
  {
    "name": "user-activity",
    "displayName": "User Activity Report",
    "description": "Shows user login and activity metrics"
  },
  {
    "name": "system-health",
    "displayName": "System Health Report",
    "description": "System performance and health metrics"
  }
]
```

#### POST /api/plugins/reporting/reports/{name}

Generates a specific report.

**Response (200):**

```json
{
  "report": {
    "name": "user-activity",
    "generatedAt": "2025-06-08T12:00:00Z",
    "data": { ... }
  }
}
```

### User Audit Plugin

All endpoints under `/api/plugins/useraudit/`.

#### GET /api/plugins/useraudit/audit

Returns the audit trail entries.

```json
[
  {
    "id": 1,
    "user": "admin@shell.com",
    "action": "LOGIN",
    "timestamp": "2025-06-08T10:00:00Z"
  }
]
```

---

## Plugin System Endpoints

### GET /api/plugins

Returns the list of loaded plugins and their status.

**Response (200):**

```json
[
  {
    "id": "reporting",
    "name": "Reporting Plugin",
    "version": "1.0.0",
    "status": "Active",
    "loadedAt": "2025-06-08T10:00:00Z"
  },
  {
    "id": "useraudit",
    "name": "User Audit Plugin",
    "version": "1.0.0",
    "status": "Active",
    "loadedAt": "2025-06-08T10:00:01Z"
  }
]
```

---

## Error Responses

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."],
    "Password": ["The Password field is required."]
  }
}
```

### 401 Unauthorized

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 403 Forbidden

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Insufficient permissions to perform this action."
}
```

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

### 500 Internal Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred."
}
```

---

## Scalar / OpenAPI

The API reference is available at:

```
http://localhost:5000/scalar
```

The OpenAPI document is served at:

```
http://localhost:5000/openapi/v1.json
```