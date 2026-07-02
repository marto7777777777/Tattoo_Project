# Tattoo Project Frontend

React/Vite frontend for the ASP.NET Core Tattoo Project backend.

## Run

```bash
npm install
npm run dev
```

Backend base URL is configured in:

```text
src/api/apiConfig.js
```

Default backend URL:

```text
https://localhost:7115
```

## Important backend note

The current backend DTOs for artists and tattoo requests do not return the database `Id` field in list/detail responses. The frontend includes fallback/manual ID inputs on workflow pages that need IDs. For a fully automatic click-through workflow, add `Id` to:

- `GetTattooArtistDto`
- `GetTattooRequestDto`
- `TattooRequestDto`

and map it in the backend services.
