# Brentwood Hills Primitive Baptist Church Website

A modern website for Brentwood Hills Primitive Baptist Church (bhpbc.org), replacing an aging Django platform. Provides static page management, sermon hosting, and a podcast RSS feed.

## Development Setup

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker + Docker Compose

### Running Locally

1. **Start the database**
   ```bash
   docker compose up -d
   ```

2. **Start the backend**
   ```bash
   cd server/src/ChurchWebsite.Api
   dotnet run
   ```
   The API will be available at `http://localhost:5001`.

3. **Start the frontend** (in a separate terminal)
   ```bash
   cd app
   npm run dev
   ```
   The Vite dev server will proxy `/api` and `/podcast/rss` to the backend.

4. **Log in**
   - Default admin credentials: `admin` / `testing123`
   - Log in at `http://localhost:5173/login`

### Smoke Testing

Before opening pull requests, verify the podcast RSS feed returns valid XML:

```bash
curl -s http://localhost:5001/podcast/rss | head -n 5
```

## Production Deployment

### Reverse Proxy Routing

The podcast RSS feed lives at `/podcast/rss` — **not** under `/api`. Your production reverse proxy (e.g., nginx, Traefik, or a cloud load balancer) must route this path to the backend, not to the static frontend files.

**Example nginx configuration:**

```nginx
server {
    listen 80;
    server_name bhpbc.org;

    location / {
        root /var/www/app/dist;
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5001/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /podcast/rss {
        proxy_pass http://localhost:5001/podcast/rss;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

If `/podcast/rss` is served by the frontend, RSS readers will receive HTML instead of XML.

## Tech Stack

- **Backend:** .NET 10, FastEndpoints v8, Dapper, PostgreSQL, JWT authentication
- **Frontend:** Vue 3 (Composition API), TypeScript, Vite, Quasar v2, Pinia, Axios
- **Database:** PostgreSQL 16 (Docker container)

## Contributing

1. Create a feature branch from `main`
2. Make focused commits with conventional commit messages
3. Open a pull request on GitHub
4. Smoke test via curl before opening PRs
