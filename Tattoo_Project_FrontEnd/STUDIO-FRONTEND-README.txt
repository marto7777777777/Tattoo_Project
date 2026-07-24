INKROUTE FRONTEND - STUDIO SYSTEM REBUILD

This frontend was rebuilt from the user's public(9).zip and aligned to the current backend Studio/TattooArtist contracts.

Core onboarding flow:
1. Open Create Artist Profile.
2. The artist MUST choose one of two cards before the form appears:
   - CREATE MY STUDIO (StudioSetupMode = 0)
   - JOIN STUDIO (StudioSetupMode = 1)
3. CREATE MY STUDIO shows separate artist fields and Studio Information fields.
4. JOIN STUDIO shows a live studio search using GET /api/Studio/join-search?query=...
5. The POST /api/TattooArtist/profile payload matches CreateTattooArtistDto exactly:
   description, phoneNumber, consultationDurationMinutes,
   offersOnlineConsultation, requiresDeposit, depositAmount,
   studioSetupMode, studio, joinStudioId,
   requirements, portfolioImages, schedules.
6. After creation, the returned JWT is saved before profile/portfolio images are uploaded.

Studio UI:
- Explore is studio-based using GET /api/Studio.
- Studio profile page lists the studio's artists in backend order.
- Clients choose a specific artist before creating a tattoo request.
- My Studio uses GET /api/Studio/mine.
- Owner controls: open/close applications, accept/reject requests, remove members, edit studio.
- Non-owner members receive a read-only studio management view.
- Pending join artists see their pending request state.

Important separation:
- Artist description/phone/portfolio/schedule remain personal.
- Studio information is edited only through My Studio by the owner.
- Profile Settings no longer calls the removed old /api/Profile/studio/studio-* endpoints.

Build note:
Dependency installation could not be completed inside the artifact runtime, so run locally:
  npm.cmd install
  npm.cmd run dev
or:
  npm.cmd run build
