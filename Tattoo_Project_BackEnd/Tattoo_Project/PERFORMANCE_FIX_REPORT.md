# InkRoute backend performance pass

## Evidence addressed

- `POST /api/TattooRequest/with-images`: SQL completed in tens of milliseconds,
  while image validation/re-encoding occupied roughly nine seconds.
- `POST /api/Auth/login`: the observed gap was inside password verification;
  role lookup was also duplicated.
- `GET /api/Studio` and `GET /api/Studio/{id}`: the old read path materialized a
  full, split EF graph (artists, users, reviews, portfolio, styles and subscriptions).
- `GET /api/ClientFavoriteStudio/my-favorites`: Explore downloaded full studio
  DTOs only to derive favorite IDs, and the full Favorites page repeated the same
  heavy EF graph.

## Changes

- Added `StudioReadService`, a shared lean read-model loader used by Explore,
  studio details and Favorites. It selects only DTO fields, aggregates ratings in
  SQL and loads portfolio/styles with bounded projection queries.
- Public studio visibility is now identical in Explore and Favorites: a studio
  must contain an artist with a currently valid trial, active period or grace period.
- Added `GET /api/ClientFavoriteStudio/my-favorite-ids` for card heart state.
  The full `my-favorites` endpoint remains available for the Saved Studios page.
- JWT token-version validation now selects only `TokenVersion`, rather than a full
  Identity user row. Immediate token revocation semantics are preserved (no cache).
- Login performs one identity lookup based on login shape, records password
  verification duration, and reuses the already loaded roles when creating the JWT.
- Valid canonical WebP uploads now use a secure fast path: full ImageSharp decode,
  dimension/pixel validation, exact RIFF-length validation, strict allowed-chunk
  validation, no metadata, no animation and no trailing payload. Other inputs retain
  the original metadata-stripping WebP re-encode fallback.
- Media storage writes `ReadOnlyMemory<byte>` directly to an asynchronous file stream,
  avoiding an extra full byte-array copy.
- Request cancellation now reaches the public studio reads, Favorites reads and the
  tattoo-request image pipeline.
- Added structured duration logs without passwords, hashes, tokens, signed URLs or
  other personal data.

## Compatibility

- Existing routes and response DTOs are unchanged.
- The only additive route is `my-favorite-ids`.
- No database migration is required.
- Subscription, rating, ordering, private-media and account-security rules remain in place.
- JWT role collections accept the `IList<string>` returned by ASP.NET Identity through
  an `IEnumerable<string>` contract.
- Studio style aggregation uses the projected `StyleRow.ArtistId` member.

## Required verification

Run from the backend directory:

```powershell
dotnet restore
dotnet build .\Tattoo_Project.csproj -c Release
dotnet test .\Tests\InkRoute.Backend.Tests.csproj -c Release
dotnet test .\ReleaseTests\InkRoute.Backend.ReleaseTests.csproj -c Release
```

After deployment, compare the same Application Insights operations and inspect the
new image validation, storage and password-verification duration logs.
