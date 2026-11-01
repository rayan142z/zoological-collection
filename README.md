# Zoolog: Zoological Collection Manager

A web app for managing zoological specimen collections. You can catalogue specimens with taxonomy, photos and the location where they were found, organise them into public or private collections, search across collections with taxonomy, weight, size and map-radius filters, and lend specimens to other users. Moderators review new taxonomy entries before they become public.

The project started as a university group project (Uni Siegen, Group 6).

**▶ Live demo:** https://rayan142z.github.io/zoological-collection/ (active once GitHub Pages is enabled, see below)

The demo runs entirely in the browser and needs no backend. It ships with sample data (6 collections, 32 specimens, loans and pending taxonomy requests), and every change stays in the visitor's own browser. Use **Reset demo** in the banner to start over. One-click demo accounts are on the login page:

| Account     | Role      | What to try                                   |
|-------------|-----------|-----------------------------------------------|
| `besucher`  | user      | own collections, favourites, lending, loans   |
| `kuratorin` | moderator | taxonomy request queue ("Requests")           |
| `admin`     | admin     | full access                                   |

The interface is available in German and English (DE/EN switch in the navigation bar).

## Screenshots

<img width="1794" height="960" alt="Screenshot_18-10-2026_214849_localhost" src="https://github.com/user-attachments/assets/27985f16-1a1b-415a-88f1-731580819e30" />
<img width="1794" height="960" alt="Screenshot_18-10-2026_214841_localhost" src="https://github.com/user-attachments/assets/b521add6-922a-45da-8eaf-913174d97755" />
<img width="1794" height="1947" alt="Screenshot_18-10-2026_214828_localhost" src="https://github.com/user-attachments/assets/711401f2-1900-44b0-82b7-d17b5e422312" />
<img width="1794" height="889" alt="Screenshot_18-10-2026_214812_localhost" src="https://github.com/user-attachments/assets/ca473a90-3c9e-4247-9af7-a654e1a0c321" />
<img width="1794" height="1049" alt="Screenshot_18-10-2026_214748_localhost" src="https://github.com/user-attachments/assets/c9235f70-e88f-4dca-befe-20da094c1904" />

## Project structure

| Folder         | What it is                                                                 |
|----------------|----------------------------------------------------------------------------|
| `zoolog-web/`  | Angular 21 frontend (standalone components, signals, Leaflet maps, Vitest) |
| `backend/`     | ASP.NET Core (.NET 10) Web API, EF Core + SQL Server, JWT auth             |
| `ZoologAPP/`   | .NET MAUI mobile/desktop client for the same API                           |
| `pdfs/`        | Project documents (specification, user manuals, work plan)                 |

## Running locally

**Frontend only (demo mode, no backend needed):**

```bash
cd zoolog-web
npm install
npm run start:demo
```

**Full stack:** see [inst.md](inst.md) and [backend/Zoolog/SETUP.md](backend/Zoolog/SETUP.md). Database credentials and the JWT key are set with `dotnet user-secrets` or environment variables and are never committed.

```bash
cd zoolog-web
npm install
npm start          # talks to the API on http://localhost:5227
```

## Deployment (GitHub Pages)

`.github/workflows/deploy-demo.yml` builds the demo (`ng build --configuration demo`) and publishes it on every push to `main` that touches `zoolog-web/`. One-time setup: **Settings → Pages → Source: GitHub Actions**.
