# Zoologische_Sammlung

To build and run:

```bash
cd backend/Zoolog
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>"
dotnet run --urls "http://localhost:5227"
```

```bash
cd zoolog-web
npm install
ng serve
```

Without a backend: `npm run start:demo` starts the frontend with built-in sample data (see README).

Login data for the shared team database: ask the team / see `backend/Zoolog/SETUP.md`.
