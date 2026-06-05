# Contact Form — Full-Stack App

Angular 17 frontend · .NET 8 Web API backend · GitHub Actions CI/CD · Azure App Service

---

## Project Structure

```
fullstack-app/
├── backend/                  # .NET 8 Web API
│   ├── Controllers/
│   ├── Models/
│   ├── Validators/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
├── frontend/                 # Angular 17 (standalone components)
│   ├── src/
│   │   ├── app/
│   │   │   ├── contact-form/   # Form component
│   │   │   ├── models/
│   │   │   ├── services/
│   │   │   ├── app.component.ts
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   ├── environments/
│   │   └── index.html
│   ├── nginx.conf
│   └── Dockerfile
├── .github/workflows/
│   └── ci-cd.yml             # GitHub Actions pipeline
└── docker-compose.yml        # Local dev with Docker
```

---

## Quick Start (Local)

### Option A — Docker Compose (recommended)

```bash
cd fullstack-app
docker-compose up --build
```

- Frontend: http://localhost:4200  
- Backend:  http://localhost:5000/swagger

### Option B — Run separately

**Backend**
```bash
cd backend
dotnet run
# API at http://localhost:5000
# Swagger at http://localhost:5000/swagger
```

**Frontend**
```bash
cd frontend
npm install
npm start
# App at http://localhost:4200
```

---

## Azure Deployment

### Prerequisites

1. Two Azure App Services (one for API, one for frontend/nginx).
2. Download the **Publish Profile** for each from the Azure portal.

### GitHub Secrets to configure

| Secret | Value |
|---|---|
| `AZURE_API_APP_NAME` | Name of the API App Service |
| `AZURE_API_PUBLISH_PROFILE` | Publish profile XML (API) |
| `AZURE_FRONTEND_APP_NAME` | Name of the Frontend App Service |
| `AZURE_FRONTEND_PUBLISH_PROFILE` | Publish profile XML (Frontend) |

### After setting secrets

Update these two files with your real App Service URLs:
- `backend/appsettings.json` → `AllowedOrigins`
- `frontend/src/environments/environment.prod.ts` → `apiBaseUrl`

Then push to `main` — the GitHub Actions pipeline builds and deploys both apps automatically.

---

## CI/CD Pipeline

```
push to main
  ├── build-backend  (dotnet restore → build → test → publish)
  ├── build-frontend (npm ci → lint → test → ng build --prod)
  ├── deploy-backend  → Azure Web App (API)   [on main only]
  └── deploy-frontend → Azure Web App (UI)   [on main only]
```

Pull requests trigger build + test only — no deployment.

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/contact/submit` | Submit the contact form |
| `GET`  | `/api/contact/health` | Health check |
| `GET`  | `/swagger`            | Swagger UI (dev only) |
