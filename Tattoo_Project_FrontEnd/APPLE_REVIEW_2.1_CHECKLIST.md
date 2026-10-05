# InkRoute — Apple App Review 2.1 Resubmission Checklist

This file is for the second App Review submission after the Guideline 2.1 information-needed rejection.

## Build

- iOS marketing version: 1.0
- iOS build number: 2
- Bundle ID: com.inkroute.app
- Artist subscription product ID: inkroute_artist_monthly_ios
- AI Project Pass product ID: inkroute_ai_project_pass_ios

## Required physical-device recording

Record on a physical iPhone running the latest supported iOS version. Start from the Home Screen and launch InkRoute.

Recommended flow:
1. Launch InkRoute.
2. Sign in with the App Review Artist account.
3. Show the Artist profile and Studio.
4. Open tattoo requests and show an existing review/test request.
5. Show Artist Response / consultation flow.
6. Show Artist Calendar and session workflow.
7. Open AI Studio.
8. Demonstrate the paid AI Project Pass purchase screen (do not use a real production purchase during recording unless intentionally testing it).
9. Open Profile / Settings.
10. Open Account Deletion and show the complete deletion flow up to the final confirmation; do not actually delete the review account.
11. Open a public Artist profile from a separate test account/session and show the Report Artist flow.
12. In the Admin account, open Artist Reports and review the report.
13. Demonstrate the Block Artist decision.
14. Show the blocked Artist profile state.
15. Show the blocked Artist uploading replacement portfolio images.
16. Submit the reinstatement request.
17. In Admin, open Reinstatement and show the submitted images.
18. Approve & unblock the Artist.

## Review account credentials

Provide current credentials in App Store Connect Sign-In Information. If multiple roles are required for review, provide both accounts in Review Notes.

Artist account:
- Username: <ADD CURRENT REVIEW ARTIST EMAIL>
- Password: <ADD CURRENT REVIEW ARTIST PASSWORD>

Client account:
- Username: <ADD CURRENT REVIEW CLIENT EMAIL>
- Password: <ADD CURRENT REVIEW CLIENT PASSWORD>

Admin account:
- Username: <ADD CURRENT REVIEW ADMIN EMAIL IF APP REVIEW NEEDS ADMIN FLOW>
- Password: <ADD CURRENT REVIEW ADMIN PASSWORD>

## App purpose / target audience

InkRoute is a workflow platform designed for tattoo artists, tattoo studios, and their clients. It replaces scattered communication and disconnected tools with one structured workflow covering tattoo requests, artist responses, consultations, scheduling, tattoo sessions, and project completion. Clients can submit structured tattoo requests and manage their tattoo journey. Artists can manage requests, consultations, availability, sessions, studio information, and ongoing projects. InkRoute also provides AI-powered tools for creating and refining tattoo concepts.

## Main feature access

1. Launch the app.
2. Sign in using the provided App Review account.
3. Artist features are available from the main navigation and Artist workspace.
4. Tattoo requests can be reviewed from My Requests / Artist Requests.
5. Consultations and sessions are accessible from the relevant tattoo request.
6. The Artist Calendar shows availability and booked sessions.
7. AI Studio is available from the AI section.
8. Profile and account deletion are available from Profile / Settings.
9. Artist moderation/reporting is available from public Artist profiles and the Admin Control Center.

## External services

- Apple StoreKit / App Store In-App Purchase — iOS Artist subscription and AI Project Pass.
- Google Play Billing — Android purchases.
- Stripe — supported web payment flows; not used as the iOS payment method.
- OpenAI — AI tattoo image generation and editing.
- Azure SQL Database — application data storage.
- Cloudflare — web/application delivery and hosting infrastructure.
- SMTP email service — account verification and transactional email, using the configured production SMTP provider.

## Regional behavior

InkRoute's core functionality is consistent across supported regions. Platform payment processing differs by platform: Apple In-App Purchase on iOS, Google Play Billing on Android, and Stripe on supported web flows. Availability and local payment/tax behavior may vary by platform or region.

## UGC moderation

InkRoute allows artists to publish profile and portfolio content. Clients and artists can report an artist profile. Reports are sent to an InkRoute administrator and do not automatically block the reported artist. An administrator can dismiss the report or block the artist profile. A blocked artist can submit replacement portfolio content for administrator review. The replacement content remains private until approved. An administrator can approve and unblock the artist or reject the submission.

## Important App Store Connect actions before resubmission

- Create/verify the iOS AI Project Pass as an In-App Purchase using product ID `inkroute_ai_project_pass_ios`.
- Add both `InkRoute Artist Monthly` and the AI Project Pass to the iOS version submission as required.
- Ensure both products are in a reviewable state and submitted alongside the app version.
- Ensure the App Review account has access to the features being demonstrated.
- Replace all placeholder credentials in this document before copying the Review Notes.
- Upload screenshots captured from the actual iOS/iPadOS app build, not browser emulation.
