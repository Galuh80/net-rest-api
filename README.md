# RestAPI

A REST API for **Product** and **Category** management built with **ASP.NET Core 8.0**, featuring JWT authentication, Redis caching, PostgreSQL, and Docker containerization.

---

## Features

- **JWT Authentication** — Register & Login with BCrypt password hashing
- **CRUD** — Full Create, Read, Update, Delete for Category and Product
- **Image Upload** — Product image upload support (multipart/form-data)
- **Pagination, Filter & Search** — All list endpoints support pagination, search, filter, and sorting
- **Redis Caching** — GET response caching with automatic invalidation on data changes
- **Webhook** — Send event notifications (create/update/delete) to an external URL
- **Swagger UI** — Interactive API documentation with Bearer token support
- **Health Check** — `/health` endpoint for monitoring
- **Docker** — App, PostgreSQL, Redis, and Nginx in a single `docker-compose.yml`
- **HTTPS** — Nginx + Let's Encrypt configuration ready to use

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 8.0 |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Cache | Redis 7 |
| Auth | JWT Bearer + BCrypt |
| Proxy | Nginx |
| SSL | Let's Encrypt (Certbot) |
| Container | Docker + Docker Compose |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/)
- PostgreSQL (if running locally without Docker)
- Redis (if running locally without Docker)

---

## Installation & Running

### Option 1 — Docker (Recommended)

```bash
# Clone the repository
git clone <repo-url>
cd RestAPI

# Start all services
docker compose up -d --build

# Check container status
docker compose ps
```

The application will be available at `http://localhost:80`

```bash
# Stop containers
docker compose down

# Stop and remove all data (volumes)
docker compose down -v
```

---

### Option 2 — Local (without Docker)

**1. Make sure PostgreSQL and Redis are running locally**

**2. Update `appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=restapi_db;Username=postgres;Password=yourpassword",
    "Redis": "localhost:6379"
  }
}
```

**3. Run database migrations**
```bash
dotnet ef database update
```

**4. Start the application**
```bash
dotnet run
```

Application runs at `http://localhost:5015`
Swagger UI available at `http://localhost:5015/swagger`

---

## Configuration

Update `appsettings.json` for production:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=restapi_db;Username=restapi_user;Password=restapi_pass",
    "Redis": "redis:6379"
  },
  "Webhook": {
    "Url": "https://your-webhook-url"
  },
  "Jwt": {
    "Key": "ReplaceWithAStrongSecretKey",
    "Issuer": "RestAPI",
    "Audience": "RestAPIUsers",
    "ExpireMinutes": 60
  }
}
```

---

## Webhook

A `POST` request is sent to the URL configured in `Webhook:Url` whenever data changes.

| Event | Trigger |
|-------|---------|
| `category.created` | New category created |
| `category.updated` | Category updated |
| `category.deleted` | Category deleted |
| `product.created` | New product created |
| `product.updated` | Product updated |
| `product.deleted` | Product deleted |

**Example payload**
```json
{
  "event": "product.created",
  "timestamp": "2026-03-29T11:00:00Z",
  "data": {
    "id": "f24fdc5d-...",
    "name": "T-Shirt",
    "price": 150000,
    "categoryId": "...",
    "imageUrl": "/uploads/products/xxx.jpg",
    "createdAt": "2026-03-29T11:00:00Z",
    "updatedAt": "2026-03-29T11:00:00Z"
  }
}
```

---

## Project Structure

```
RestAPI/
├── src/
│   ├── Constantas/          # Response wrappers, interfaces, DTOs, QueryParams
│   ├── Controllers/         # AuthController, CategoryController, ProductController
│   ├── Models/              # User, Product, Category
│   ├── Services/            # Auth, Category, Product, Webhook
│   └── Infrastructures/     # AppDbContext, Migrations
├── nginx/
│   └── nginx.conf           # Reverse proxy + HTTPS configuration
├── Dockerfile
├── docker-compose.yml
├── appsettings.json
└── Program.cs
```

---

## License

MIT
