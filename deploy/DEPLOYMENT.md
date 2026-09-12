# Deployment

The site is built in GitHub Actions and deployed to Linode VPS servers via rsync over SSH. The server runs:

- **nginx** — serves the static Vue build and reverse-proxies `/api`, `/podcast/rss`, and `/uploads` to the API
- **ChurchWebsite.Api** — .NET 10 self-contained binary running as a systemd service on `127.0.0.1:5001`
- **PostgreSQL 16** — Docker container (managed by Compose), bound to `127.0.0.1:5432`; schema created automatically by `DbInitializer` on first startup

Postgres is the **only** thing containerized on the server — chosen so it stays isolated from the other apps on this shared VPS (own version, own data volume, own lifecycle). The app itself runs natively (API binary + static frontend).

---

## 1. One-time server setup (dev server)

Run everything as `root` unless noted. Commands are for Ubuntu.

### 1.1 Hostname / DNS

Add a DNS `A` record for `dev.bhpbc.org` (or whatever dev domain you use) pointing at the server's IP. If you're not using a domain yet, you can put the server IP directly in `server_name` instead.

### 1.2 Base packages

```bash
apt update && apt upgrade -y
apt install -y nginx rsync ufw curl docker.io docker-compose-v2
systemctl enable --now docker
```

> On a shared VPS, avoid exposing Postgres directly. The container below binds to `127.0.0.1` only.

### 1.3 GitHub Actions SSH access

Create a dedicated keypair for the pipeline. **Do not use the deployer user's personal key.**

```bash
sudo -u deployer mkdir -p /home/deployer/.ssh
sudo -u deployer ssh-keygen -t ed25519 -f /home/deployer/.ssh/github-actions -N "" -C "github-actions-deploy"
sudo -u deployer bash -c 'cat /home/deployer/.ssh/github-actions.pub >> /home/deployer/.ssh/authorized_keys'
chmod 700 /home/deployer/.ssh
chmod 600 /home/deployer/.ssh/authorized_keys /home/deployer/.ssh/github-actions
```

Copy the **private** key (`/home/deployer/.ssh/github-actions`) into the GitHub Actions secret later (see section 2).

Allow the deployer to restart the API without a password:

```bash
cat > /etc/sudoers.d/church-website-deploy <<'EOF'
deployer ALL=(root) NOPASSWD: /usr/bin/systemctl restart church-website-api
EOF
chmod 440 /etc/sudoers.d/church-website-deploy
```

### 1.4 Application directories

```bash
mkdir -p /opt/church-website/{server,app,storage/{audio,images,transcripts}}
chown -R deployer:deployer /opt/church-website
chmod 750 /opt/church-website/storage
```

### 1.5 PostgreSQL (Docker container)

```bash
mkdir -p /opt/church-website/postgres
cp /home/plowrance/church-website/deploy/postgres/docker-compose.yml /opt/church-website/postgres/

# The DB password lives in a local .env next to the compose file (not in git)
cat > /opt/church-website/postgres/.env <<'EOF'
POSTGRES_PASSWORD=CHANGE_ME_DB_PASSWORD
EOF
chmod 600 /opt/church-website/postgres/.env

docker compose -f /opt/church-website/postgres/docker-compose.yml up -d
docker compose -f /opt/church-website/postgres/docker-compose.yml ps   # confirm healthy
```

This creates a `church_website` database owned by user `church_website`, reachable only at `127.0.0.1:5432`. Data is stored in the named volume `church_website_pgdata`, so the container can be upgraded/recreated without losing data, and `restart: unless-stopped` brings it back on boot.

**Backups** (do this early — a shared host has no protection from `docker compose down -v`):

```bash
mkdir -p /var/backups/church-website
docker exec church-website-postgres pg_dump -U church_website -d church_website \
  > /var/backups/church-website/$(date +%F).sql
```

Add it to root's crontab (`crontab -e`): `30 3 * * * docker exec church-website-postgres pg_dump -U church_website -d church_website > /var/backups/church-website/$(date +\%F).sql`

### 1.6 Application configuration

Copy the template to the deployed app directory. The pipeline **never overwrites** this file (rsync excludes `appsettings.Production.json`), so your secrets survive every deploy.

```bash
cp /home/plowrance/church-website/deploy/appsettings.Production.json.example \
   /opt/church-website/server/appsettings.Production.json
chown deployer:deployer /opt/church-website/server/appsettings.Production.json
chmod 600 /opt/church-website/server/appsettings.Production.json
```

Edit it and set real values:

- `ConnectionStrings:DefaultConnection` — the Postgres password from the `.env` in 1.5
- `Jwt:Key` — a long random string (e.g. `openssl rand -base64 48`)
- `AssemblyAI:ApiKey` — real key (or supply via `/etc/church-website/api.env` instead; see 1.7)
- `Podcast:BaseUrl` — the dev domain, e.g. `https://dev.bhpbc.org`
- `Storage:*Path` — leave the `/opt/church-website/storage/...` absolute paths as-is

### 1.7 (Optional) Secrets via environment file

If you prefer to keep the AssemblyAI key out of the config file entirely, create `/etc/church-website/api.env`:

```bash
mkdir -p /etc/church-website
cat > /etc/church-website/api.env <<'EOF'
AssemblyAI__ApiKey=your-real-key
EOF
chmod 600 /etc/church-website/api.env
```

Then uncomment `EnvironmentFile=/etc/church-website/api.env` in the systemd unit (section 1.8).

### 1.8 systemd service

```bash
cp /home/plowrance/church-website/deploy/systemd/church-website-api.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable church-website-api   # enable now; the binary arrives on first deploy
```

The first deploy rsyncs the API binary into `/opt/church-website/server/` and then restarts the service, which runs `DbInitializer` (creates the schema and seeds the admin user). Watch the first boot:

```bash
journalctl -u church-website-api -f
```

### 1.9 nginx

```bash
cp /home/plowrance/church-website/deploy/nginx/church-website.conf /etc/nginx/sites-available/church-website
ln -s /etc/nginx/sites-available/church-website /etc/nginx/sites-enabled/church-website
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl enable --now nginx
systemctl reload nginx
```

Edit `/etc/nginx/sites-available/church-website` first if your dev domain differs from `dev.bhpbc.org`.

### 1.10 Firewall

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
```

---

## 2. GitHub configuration

### 2.1 Repository secrets

Create two **Environments** in the repo (Settings → Environments): `dev` and `prod`.

Add these secrets to each environment (same names, different values per environment):

| Secret      | Value                                              |
|-------------|----------------------------------------------------|
| `HOST`      | Server IP or hostname (e.g. `203.0.113.10`)        |
| `SSH_USER`  | `deployer`                                         |
| `SSH_PORT`  | `22`                                               |
| `SSH_KEY`   | The **private** key from section 1.3 (PEM/OpenSSH format, including the `-----BEGIN OPENSSH PRIVATE KEY-----` block) |

### 2.2 Protect production (recommended)

In the `prod` environment settings, add **Required reviewers** so production deploys require an approval before they start.

---

## 3. Running a deploy

- **Dev:** merge any PR into `main` — the workflow builds and deploys to dev automatically.
- **Prod:** go to Actions → **Deploy** → *Run workflow* → choose `environment: prod`. The workflow builds fresh artifacts and deploys them to the prod server.

Both environments use the same build step, so prod always gets exactly what dev got.

### What the workflow does

1. `dotnet publish` the API (self-contained `linux-x64`, so the server needs no .NET runtime)
2. `npm ci && npm run build` the frontend
3. rsync the API to `/opt/church-website/server/` (excluding `appsettings.Production.json`)
4. rsync the frontend to `/opt/church-website/app/`
5. Seeds the default branding SVGs into storage if missing
6. `sudo systemctl restart church-website-api`
7. Health-checks `http://127.0.0.1:5001/api/site-info`

## 4. Verifying

```bash
curl -s http://dev.bhpbc.org/api/site-info
curl -s http://dev.bhpbc.org/podcast/rss | head -n 5
curl -sI http://dev.bhpbc.org/ | head -n 5
journalctl -u church-website-api -n 50
```

## 5. Production notes (when you get there)

- Repeat section 1 on the prod VPS, using the prod domain in nginx and `Podcast:BaseUrl`.
- Provision a **different** Postgres password and `Jwt:Key` for prod.
- Install a TLS cert: `apt install certbot python3-certbot-nginx && certbot --nginx -d bhpbc.org`.
- Add `server_name bhpbc.org` (and any www variant) to the nginx config and swap the `Podcast:BaseUrl`.
- Change the seeded admin password after first login:
  ```bash
  docker exec -it church-website-postgres psql -U church_website -d church_website
  UPDATE users SET password_hash = crypt('NEW_STRONG_PASSWORD', gen_salt('bf')) WHERE username = 'admin';
  ```
  (requires `CREATE EXTENSION pgcrypto;` if not already available)
- Keep the `pg_dump` backup cron from section 1.5 running, and periodically test a restore.