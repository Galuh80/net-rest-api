# Functional Specification Document (FSD)

**Project Name:** RestAPI — Product & Category Management  
**Version:** 1.0.0  
**Date:** 2026-04-10  
**Status:** Active  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Objectives](#2-objectives)
3. [System Architecture](#3-system-architecture)
4. [Actors & Roles](#4-actors--roles)
5. [Functional Requirements](#5-functional-requirements)
   - 5.1 [Authentication](#51-authentication)
   - 5.2 [Category Management](#52-category-management)
   - 5.3 [Product Management](#53-product-management)
   - 5.4 [Pagination, Search & Filter](#54-pagination-search--filter)
   - 5.5 [Image Upload](#55-image-upload)
   - 5.6 [Webhook](#56-webhook)
   - 5.7 [Health Check](#57-health-check)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Data Models](#7-data-models)
8. [API Specification](#8-api-specification)
9. [Response Format](#9-response-format)
10. [Error Handling](#10-error-handling)
11. [Caching Strategy](#11-caching-strategy)
12. [Infrastructure](#12-infrastructure)
13. [Security](#13-security)
14. [Constraints & Assumptions](#14-constraints--assumptions)

---

## 1. Overview

This document describes the functional specification of the **RestAPI** system, a backend REST API service built with **ASP.NET Core 8.0**. The system provides endpoints for managing **Products** and **Categories**, secured with **JWT authentication**, backed by **PostgreSQL**, cached via **Redis**, and deployed using **Docker** with **Nginx** as a reverse proxy.

---

## 2. Objectives

- Provide a secure, scalable REST API for managing Product and Category data
- Enforce authentication on all data endpoints using JWT tokens
- Support efficient data retrieval via pagination, search, and filtering
- Allow product image uploads stored on the server
- Notify external systems via webhook on any data mutation event
- Serve the API behind a production-grade Nginx reverse proxy with HTTPS support

---

## 3. System Architecture

```
Client (Browser / Mobile / Postman)
            │
            ▼
      Nginx (Port 80/443)
      Reverse Proxy + SSL
            │
            ▼
    ASP.NET Core App (Port 8080)
    ┌───────────────────────────┐
    │  Controllers              │
    │  Services (Business Logic)│
    │  Entity Framework Core    │
    └───────┬───────────┬───────┘
            │           │
      PostgreSQL      Redis
      (Persistence)  (Cache)
            
    WebhookService ──► External Webhook URL
```

### Components

| Component | Technology | Role |
|-----------|------------|------|
| API Server | ASP.NET Core 8.0 | Business logic, routing, auth |
| Database | PostgreSQL 16 | Persistent data storage |
| Cache | Redis 7 | Response caching |
| Proxy | Nginx (Alpine) | Reverse proxy, SSL termination |
| SSL | Let's Encrypt (Certbot) | HTTPS certificate management |
| Container | Docker + Docker Compose | Service orchestration |

---

## 4. Actors & Roles

| Actor | Description |
|-------|-------------|
| **Guest** | Unauthenticated user. Can only access `/api/auth/register` and `/api/auth/login` |
| **Authenticated User** | Registered and logged-in user. Has full access to Category and Product endpoints |
| **External System** | Any external service that receives webhook notifications from this API |

---

## 5. Functional Requirements

### 5.1 Authentication

#### FR-AUTH-01: User Registration

| | |
|---|---|
| **Endpoint** | `POST /api/auth/register` |
| **Access** | Public |
| **Description** | Allows a new user to create an account |

**Input:**
```json
{
  "name": "string (required, 2–100 chars)",
  "email": "string (required, valid email format)",
  "password": "string (required, min 6 chars)"
}
```

**Business Rules:**
- Email must be unique in the system
- Password is hashed using BCrypt before storage
- Returns `409 Conflict` if email already exists

---

#### FR-AUTH-02: User Login

| | |
|---|---|
| **Endpoint** | `POST /api/auth/login` |
| **Access** | Public |
| **Description** | Authenticates user and returns a JWT token |

**Input:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Business Rules:**
- Validates email and BCrypt-hashed password
- Returns a signed JWT token on success
- Token expires after 60 minutes (configurable)
- Returns `401 Unauthorized` if credentials are invalid

**JWT Claims:**
| Claim | Value |
|-------|-------|
| `sub` | User ID (GUID) |
| `email` | User email |
| `name` | User name |
| `jti` | Unique token ID |
| `exp` | Expiration time |

---

### 5.2 Category Management

> All endpoints require `Authorization: Bearer {token}` header.

#### FR-CAT-01: List Categories

| | |
|---|---|
| **Endpoint** | `GET /api/category` |
| **Access** | Authenticated |
| **Description** | Returns a paginated, searchable, sortable list of categories |

**Business Rules:**
- Results are cached in Redis for 5 minutes
- Cache is invalidated on any create/update/delete operation

---

#### FR-CAT-02: Get Category by ID

| | |
|---|---|
| **Endpoint** | `GET /api/category/{id}` |
| **Access** | Authenticated |
| **Description** | Returns a single category by its GUID |

**Business Rules:**
- Returns `404 Not Found` if category does not exist
- Result is cached in Redis for 5 minutes

---

#### FR-CAT-03: Create Category

| | |
|---|---|
| **Endpoint** | `POST /api/category` |
| **Access** | Authenticated |
| **Description** | Creates a new category |

**Input:**
```json
{
  "name": "string (required, 2–100 chars)"
}
```

**Business Rules:**
- Generates a new GUID as ID
- Sets `createdAt` and `updatedAt` to current UTC time
- Invalidates list cache in Redis
- Sends `category.created` webhook event

---

#### FR-CAT-04: Update Category

| | |
|---|---|
| **Endpoint** | `PUT /api/category/{id}` |
| **Access** | Authenticated |
| **Description** | Updates an existing category |

**Business Rules:**
- Returns `404 Not Found` if category does not exist
- Updates `updatedAt` to current UTC time
- Invalidates list and detail cache in Redis
- Sends `category.updated` webhook event

---

#### FR-CAT-05: Delete Category

| | |
|---|---|
| **Endpoint** | `DELETE /api/category/{id}` |
| **Access** | Authenticated |
| **Description** | Deletes a category by ID |

**Business Rules:**
- Returns `404 Not Found` if category does not exist
- Invalidates list and detail cache in Redis
- Sends `category.deleted` webhook event

---

### 5.3 Product Management

> All endpoints require `Authorization: Bearer {token}` header.

#### FR-PRD-01: List Products

| | |
|---|---|
| **Endpoint** | `GET /api/product` |
| **Access** | Authenticated |
| **Description** | Returns a paginated, searchable, filterable list of products |

**Business Rules:**
- Supports filtering by `categoryId`, `minPrice`, `maxPrice`
- Results are cached in Redis for 5 minutes
- Cache is invalidated on any create/update/delete operation

---

#### FR-PRD-02: Get Product by ID

| | |
|---|---|
| **Endpoint** | `GET /api/product/{id}` |
| **Access** | Authenticated |
| **Description** | Returns a single product by its GUID |

**Business Rules:**
- Returns `404 Not Found` if product does not exist
- Result is cached in Redis for 5 minutes

---

#### FR-PRD-03: Create Product

| | |
|---|---|
| **Endpoint** | `POST /api/product` |
| **Access** | Authenticated |
| **Content-Type** | `multipart/form-data` |
| **Description** | Creates a new product with optional image upload |

**Input:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `name` | string | Yes | 2–200 characters |
| `price` | decimal | Yes | Greater than 0 |
| `categoryId` | GUID | Yes | Must exist in Categories |
| `image` | file | No | Any image file |

**Business Rules:**
- Validates that `categoryId` exists in the database
- If image is provided, saves file to `wwwroot/uploads/products/` with a GUID filename
- Stores relative image URL in `imageUrl` field
- Invalidates list cache in Redis
- Sends `product.created` webhook event

---

#### FR-PRD-04: Update Product

| | |
|---|---|
| **Endpoint** | `PUT /api/product/{id}` |
| **Access** | Authenticated |
| **Content-Type** | `multipart/form-data` |
| **Description** | Updates an existing product |

**Business Rules:**
- Returns `404 Not Found` if product does not exist
- Validates that `categoryId` exists in the database
- If a new image is uploaded, the old image file is deleted from disk
- Updates `updatedAt` to current UTC time
- Invalidates list and detail cache in Redis
- Sends `product.updated` webhook event

---

#### FR-PRD-05: Delete Product

| | |
|---|---|
| **Endpoint** | `DELETE /api/product/{id}` |
| **Access** | Authenticated |
| **Description** | Deletes a product and its associated image |

**Business Rules:**
- Returns `404 Not Found` if product does not exist
- Deletes associated image file from disk if it exists
- Invalidates list and detail cache in Redis
- Sends `product.deleted` webhook event

---

### 5.4 Pagination, Search & Filter

All list endpoints support the following base query parameters (inherited from `BaseQueryParams`):

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number (1-based) |
| `pageSize` | int | 10 | Items per page (max 100, min 1) |
| `search` | string | null | Search keyword (name field) |
| `sortBy` | string | `name` | Field to sort by |
| `sortOrder` | string | `asc` | Sort direction: `asc` or `desc` |

**Additional filters for Product:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `categoryId` | GUID | Filter products by category |
| `minPrice` | decimal | Minimum price (inclusive) |
| `maxPrice` | decimal | Maximum price (inclusive) |

**Sortable fields:**

| Model | sortBy options |
|-------|----------------|
| Category | `name`, `createdAt`, `updatedAt` |
| Product | `name`, `price`, `createdAt`, `updatedAt` |

---

### 5.5 Image Upload

| | |
|---|---|
| **Supported on** | `POST /api/product`, `PUT /api/product/{id}` |
| **Field name** | `image` |
| **Max size** | 20 MB (enforced by Nginx) |
| **Storage** | `wwwroot/uploads/products/{guid}{ext}` |
| **Access URL** | `/uploads/products/{filename}` |

**Business Rules:**
- Image field is optional on both create and update
- On update, if a new image is provided, the old image is deleted from disk
- On delete, the associated image is deleted from disk
- Filename is generated as a new GUID to prevent collisions

---

### 5.6 Webhook

The system sends an HTTP `POST` request to the configured `Webhook:Url` on every data mutation event.

#### Webhook Events

| Event | Trigger |
|-------|---------|
| `category.created` | New category created |
| `category.updated` | Category updated |
| `category.deleted` | Category deleted |
| `product.created` | New product created |
| `product.updated` | Product updated |
| `product.deleted` | Product deleted |

#### Payload Format

```json
{
  "event": "product.created",
  "timestamp": "2026-04-10T10:00:00Z",
  "data": { }
}
```

**Business Rules:**
- Webhook is sent after data is successfully saved to the database
- If webhook delivery fails, the error is logged but does not affect the API response
- Timeout for webhook delivery is 10 seconds

---

### 5.7 Health Check

| | |
|---|---|
| **Endpoint** | `GET /health` |
| **Access** | Public |
| **Description** | Returns the health status of the application |

**Response:**
```
Healthy
```
HTTP Status: `200 OK`

---

## 6. Non-Functional Requirements

| # | Requirement | Detail |
|---|-------------|--------|
| NFR-01 | **Security** | All data endpoints protected by JWT Bearer authentication |
| NFR-02 | **Password Security** | Passwords hashed with BCrypt before storage |
| NFR-03 | **Performance** | GET responses cached in Redis for 5 minutes |
| NFR-04 | **Scalability** | Stateless API design; horizontally scalable |
| NFR-05 | **Availability** | All containers set to `restart: unless-stopped` |
| NFR-06 | **HTTPS** | Nginx configured for TLS 1.2/1.3 with Let's Encrypt |
| NFR-07 | **Upload Limit** | Maximum upload size of 20MB enforced at Nginx level |
| NFR-08 | **Token Expiry** | JWT tokens expire after 60 minutes |
| NFR-09 | **Pagination Limit** | Maximum 100 items per page to prevent excessive load |

---

## 7. Data Models

### User

| Field | Type | Constraints |
|-------|------|-------------|
| `Id` | GUID | Primary Key |
| `Name` | string | Required |
| `Email` | string | Required, Unique |
| `Password` | string | Required, BCrypt hashed |
| `CreatedAt` | DateTime | Auto-set on create (UTC) |
| `UpdatedAt` | DateTime | Auto-set on update (UTC) |

### Category

| Field | Type | Constraints |
|-------|------|-------------|
| `Id` | GUID | Primary Key |
| `Name` | string | Required |
| `CreatedAt` | DateTime | Auto-set on create (UTC) |
| `UpdatedAt` | DateTime | Auto-set on update (UTC) |

### Product

| Field | Type | Constraints |
|-------|------|-------------|
| `Id` | GUID | Primary Key |
| `Name` | string | Required |
| `Price` | decimal | Required, > 0 |
| `CategoryId` | GUID | Foreign Key → Category |
| `ImageUrl` | string? | Nullable, relative path |
| `CreatedAt` | DateTime | Auto-set on create (UTC) |
| `UpdatedAt` | DateTime | Auto-set on update (UTC) |

---

## 8. API Specification

### Base URL

| Environment | URL |
|-------------|-----|
| Production | `http://galuh.biz.id` |
| Development | `http://localhost:5015` |
| Swagger UI | `http://localhost:5015/swagger` (Development only) |

### Endpoints Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Register user |
| POST | `/api/auth/login` | No | Login, get token |
| GET | `/api/category` | Yes | List categories |
| GET | `/api/category/{id}` | Yes | Get category |
| POST | `/api/category` | Yes | Create category |
| PUT | `/api/category/{id}` | Yes | Update category |
| DELETE | `/api/category/{id}` | Yes | Delete category |
| GET | `/api/product` | Yes | List products |
| GET | `/api/product/{id}` | Yes | Get product |
| POST | `/api/product` | Yes | Create product |
| PUT | `/api/product/{id}` | Yes | Update product |
| DELETE | `/api/product/{id}` | Yes | Delete product |
| GET | `/health` | No | Health check |

---

## 9. Response Format

### Standard Response

```json
{
  "success": true,
  "message": "Success",
  "data": { }
}
```

### Paginated Response

```json
{
  "success": true,
  "message": "Success",
  "data": [ ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

---

## 10. Error Handling

| HTTP Status | Scenario |
|-------------|----------|
| `400 Bad Request` | Validation failed or invalid input (e.g., CategoryId not found) |
| `401 Unauthorized` | Missing, expired, or invalid JWT token |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Email already registered |
| `500 Internal Server Error` | Unexpected server error |

**Error Response Format:**
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

---

## 11. Caching Strategy

| Operation | Cache Action | TTL |
|-----------|-------------|-----|
| `GET /api/category` | Read from cache, write on miss | 5 minutes |
| `GET /api/category/{id}` | Read from cache, write on miss | 5 minutes |
| `POST /api/category` | Invalidate list cache | - |
| `PUT /api/category/{id}` | Invalidate list + detail cache | - |
| `DELETE /api/category/{id}` | Invalidate list + detail cache | - |
| `GET /api/product` | Read from cache, write on miss | 5 minutes |
| `GET /api/product/{id}` | Read from cache, write on miss | 5 minutes |
| `POST /api/product` | Invalidate list cache | - |
| `PUT /api/product/{id}` | Invalidate list + detail cache | - |
| `DELETE /api/product/{id}` | Invalidate list + detail cache | - |

**Cache Key Format:**
- List: `RestAPI:category:all:{page}:{pageSize}:{search}:{sortBy}:{sortOrder}`
- Detail: `RestAPI:category:{id}`

---

## 12. Infrastructure

### Docker Services

| Service | Image | Port | Description |
|---------|-------|------|-------------|
| `app` | Custom (.NET 8) | 8080 (internal) | REST API application |
| `db` | postgres:16-alpine | 5432 (internal) | PostgreSQL database |
| `redis` | redis:7-alpine | 6379 (internal) | Redis cache |
| `nginx` | nginx:alpine | 80, 443 (public) | Reverse proxy |
| `certbot` | certbot/certbot | - | SSL certificate renewal |

### Volumes

| Volume | Purpose |
|--------|---------|
| `pgdata` | PostgreSQL data persistence |
| `redisdata` | Redis data persistence |
| `uploads` | Product image uploads |
| `certbot_www` | ACME challenge for Let's Encrypt |
| `certbot_certs` | SSL certificate storage |

### Database Migrations

Migrations are applied automatically on application startup using `db.Database.Migrate()`.

| Migration | Description |
|-----------|-------------|
| `20260327105437_InitialCreate` | Create Users, Categories, Products tables |
| `20260329105744_AddImageUrlToProduct` | Add `ImageUrl` column to Products |

---

## 13. Security

| Concern | Implementation |
|---------|----------------|
| Authentication | JWT Bearer tokens |
| Password storage | BCrypt hashing |
| Token expiry | 60 minutes |
| HTTPS | TLS 1.2/1.3 via Nginx + Let's Encrypt |
| Upload size limit | 20MB enforced at Nginx level |
| Internal network | All services communicate via private Docker network |
| Public exposure | Only Nginx ports 80/443 are exposed to the internet |

---

## 14. Constraints & Assumptions

- The API does not implement role-based access control (RBAC) — all authenticated users have equal access
- Image files are stored on the local filesystem; no cloud storage (S3, GCS) is used in this version
- Let's Encrypt HTTPS requires a valid public domain; IP address only supports HTTP
- The webhook URL is a single global configuration; per-event subscription is not supported
- Redis cache TTL is fixed at 5 minutes for all endpoints
- Database auto-migration runs on every application startup
