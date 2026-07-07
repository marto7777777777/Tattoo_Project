# Tattoo Project Frontend

React/Vite frontend for the tattoo booking platform.

## Run

```bash
npm install
npm run dev
```

Default backend URL is in:

```text
src/api/apiConfig.js
```

## Added in this version

- Client profile city/country fields
- Artist profile studio city/country/latitude/longitude fields
- Recommended artists endpoint integration
- Search artists by name/studio/city/country/address
- Artist rating/review display
- Favorite/unfavorite artists
- My favorites page
- Create review page for completed tattoo requests

## Important backend DTO note

For request/favorite buttons to be fully reliable, `GetTattooArtistDto` should return artist `Id`.
For review buttons to be fully reliable, `GetTattooRequestDto` should return request `Id`.
