# InkRoute Studio System – implementation notes

## What changed

The previous model stored studio data directly on every `TattooArtist`. That worked for solo artists, but duplicated the same studio information when several artists work together.

The new structure separates the studio from the artist while keeping the existing artist workflow intact.

### New backend entities

- `Studio`
  - Name
  - Description
  - Address / City / Country
  - Latitude / Longitude
  - `IsOpenForJoinRequests`
  - `OwnerArtistId`
  - CreatedOn
- `StudioJoinRequest`
  - StudioId
  - TattooArtistId
  - Pending / Accepted / Rejected / Cancelled
  - CreatedOn / RespondedOn

### TattooArtist

The artist still owns all personal data and workflow data:

- Description
- Phone number
- Verification status
- Consultation settings
- Deposit settings
- Requirements
- Portfolio
- Schedule
- Unavailable dates
- Requests / consultations / sessions

The artist now has:

- `StudioId`
- `JoinedStudioOn`

`StudioId` is nullable only because an artist who chose **Join Studio** needs to exist while the owner reviews the request. Pending artists are not shown publicly and cannot receive new tattoo requests.

## Artist onboarding

The artist profile page now asks:

1. **Create My Studio**
2. **Join Studio**

### Create My Studio

The user enters personal artist information and separate studio information.

After successful creation:

- the artist profile is created;
- the studio is created;
- that artist becomes `OwnerArtistId`;
- the artist is added as the first member;
- the studio starts with join requests open.

### Join Studio

The user searches only studios with `IsOpenForJoinRequests = true`.

After profile creation:

- the TattooArtist profile exists;
- a Pending `StudioJoinRequest` is created;
- the artist is not publicly discoverable yet;
- `/my-studio` shows the pending request.

When accepted, the artist receives `StudioId` and `JoinedStudioOn`.

## Owner permissions

Only the studio owner can:

- edit studio information;
- open / close studio applications;
- accept join requests;
- reject join requests;
- remove other studio members.

The owner cannot remove themselves.

These checks exist in the backend, not only in the React UI.

## Non-owner My Studio

Normal members can open My Studio and see:

- studio information;
- studio members;
- their existing personal Requests / Calendar links.

Studio administration is read-only for them.

## Closing applications

`Studio.IsOpenForJoinRequests` controls only artist applications.

When closed:

- the studio disappears from Join Studio search;
- new join requests are rejected by the backend even if someone calls the endpoint manually;
- existing pending requests remain and the owner can still Accept / Reject them;
- the studio remains fully visible to clients in Explore.

## Explore

Explore now lists **studios**, not individual artists.

Each studio card shows:

- studio name / location / description;
- member previews;
- combined portfolio preview;
- number of artists.

The public DTO deliberately does not expose which artist is the owner.

Artists are sorted internally as:

1. Owner first
2. Remaining artists by `JoinedStudioOn`

The client only sees the order, never the owner role.

## Studio profile

`/studios/:studioId` displays the studio and its artists.

The client chooses an individual artist there and then starts the existing tattoo-request flow for that `TattooArtistId`.

The existing logic remains artist-specific:

- Schedule
- ArtistResponse
- Consultation
- TattooSession
- TattooRequest
- Portfolio
- Requirements

No studio-level booking calendar was introduced.

## Validation / defensive checks

Backend validation includes:

- artist description required and length-limited;
- artist phone required and length-limited;
- consultation duration 15–180 minutes;
- required positive deposit when deposits are enabled;
- at least one consultation schedule;
- at least one tattoo-session schedule;
- start time before end time;
- valid schedule enums;
- duplicate schedule prevention;
- duplicate requirement prevention;
- requirement length limit;
- studio required fields and length limits;
- latitude / longitude range validation;
- duplicate exact studio name + city + address prevention;
- selected join studio must exist;
- selected join studio must be open when the request is created;
- only one pending join request per artist;
- artist cannot join when already a member;
- only owner can accept / reject / remove / edit / close applications;
- owner cannot remove themselves;
- public tattoo requests cannot be created for artists who currently have no studio.

## Admin deletion behavior

Deleting an owner no longer leaves an invalid studio owner reference.

If the studio has other members, ownership transfers to the earliest joined remaining artist.

If the studio has no other members, the studio is removed.

## Database migration

Included migration:

`20260724130000_AddStudioSystem.cs`

It preserves existing artist data by:

1. adding the new StudioId / JoinedStudioOn columns;
2. creating a Studio for every existing TattooArtist;
3. making that artist owner of the migrated studio;
4. assigning the artist to the studio;
5. only then removing the old duplicated StudioName / StudioAddress / etc. columns.

Run:

`Update-Database`

or:

`dotnet ef database update`

## Files added

### Backend

- Models/Studio.cs
- Models/StudioJoinRequest.cs
- Models/StudioJoinRequestStatus.cs
- Models/StudioSetupMode.cs
- Configuration/StudioConfiguration.cs
- Configuration/StudioJoinRequestConfiguration.cs
- DTOs/StudioDTOs/StudioDtos.cs
- Services/IStudioService.cs
- Services/StudioService.cs
- Controllers/StudioController.cs
- Migrations/20260724130000_AddStudioSystem.cs
- STUDIO-SYSTEM-SETUP.txt

### Frontend

- src/api/studioApi.js
- src/components/StudioCard.jsx
- src/pages/StudioProfilePage.jsx

Major updated pages:

- CreateArtistProfilePage.jsx
- ArtistWorkspacePage.jsx
- ArtistsPage.jsx
- ProfileSectionPage.jsx
- App.jsx
- Navbar.jsx
- index.css

## Important intentional non-changes

The existing AI implementation was not changed.

The existing tattoo workflow stays artist-based.

Stripe/payment logic was not added.

Phone verification was not implemented yet; the individual artist phone field is preserved so verification can be added later.
