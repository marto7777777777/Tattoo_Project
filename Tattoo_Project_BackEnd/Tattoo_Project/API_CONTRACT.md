# InkRoute Backend API Contract

> Generated from the controller source included in this archive. Backend code is authoritative.

## Error contract

- `400` — validation/rejected command where the existing controller returns BadRequest.
- `401` — missing/invalid JWT.
- `403` — authenticated but missing role/entitlement.
- `404` — resource unavailable to the caller.
- `409` — controlled concurrency/state conflict with ProblemDetails `code` and `correlationId`.
- `429` — rate-limiter rejection.
- `500` — unexpected server error; provider response bodies/secrets are not returned.

## Endpoints

| Method | Route | Authorization | Request DTO | Declared return type | Controller method |
|---|---|---|---|---|---|
| `DELETE` | `/api/account` | Authorize | DeleteAccountDto | `Task<IActionResult>` | `AccountController.cs::Delete` |
| `GET` | `/api/admin/overview` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::GetOverview` |
| `GET` | `/api/admin/users` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::GetUsers` |
| `GET` | `/api/admin/tattoo-requests` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::GetTattooRequests` |
| `GET` | `/api/admin/ai-projects` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::GetAiProjects` |
| `DELETE` | `/api/admin/users/{userId}` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::DeleteUser` |
| `DELETE` | `/api/admin/client-profiles/{clientId:int}` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::DeleteClientProfile` |
| `DELETE` | `/api/admin/artist-profiles/{artistId:int}` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::DeleteArtistProfile` |
| `DELETE` | `/api/admin/tattoo-requests/{tattooRequestId:int}` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::DeleteTattooRequest` |
| `DELETE` | `/api/admin/ai-projects/{projectId:int}` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::DeleteAiProject` |
| `PATCH` | `/api/admin/artist-profiles/{artistId:int}/verified` | Authorize | — | `Task<IActionResult>` | `AdminController.cs::SetArtistVerified` |
| `GET` | `/api/ai-tattoos` | Authorize | — | `Task<IActionResult>` | `AiTattooController.cs::GetMine` |
| `GET` | `/api/ai-tattoos/{id:int}` | Authorize | — | `Task<IActionResult>` | `AiTattooController.cs::Get` |
| `POST` | `/api/ai-tattoos/paid-draft` | Authorize | CreateAiTattooProjectDto | `Task<IActionResult>` | `AiTattooController.cs::CreatePaidDraft` |
| `POST` | `/api/ai-tattoos/{id:int}/generate` | Authorize | — | `Task<IActionResult>` | `AiTattooController.cs::Generate` |
| `POST` | `/api/ai-tattoos` | Authorize | CreateAiTattooProjectDto | `Task<IActionResult>` | `AiTattooController.cs::Create` |
| `POST` | `/api/ai-tattoos/{id:int}/edit` | Authorize | EditAiTattooProjectDto | `Task<IActionResult>` | `AiTattooController.cs::Edit` |
| `POST` | `/api/ai-tattoos/{id:int}/checkout` | Authorize | — | `Task<IActionResult>` | `AiTattooController.cs::Checkout` |
| `GET` | `/api/ai-tattoos/versions/{versionId:int}/download` | Authorize | — | `Task<IActionResult>` | `AiTattooController.cs::DownloadVersion` |
| `POST` | `/api/analytics/pending-events/consume` | Authorize | — | `Task<IActionResult>` | `AnalyticsController.cs::Consume` |
| `GET` | `/api/[controller]` | Authorize | — | `Task<IActionResult>` | `ArtistResponseController.cs::GetAllArtistResponses` |
| `GET` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ArtistResponseController.cs::GetArtistResponseById` |
| `GET` | `/api/[controller]/my-responses` | Authorize | — | `Task<IActionResult>` | `ArtistResponseController.cs::GetMyArtistResponses` |
| `POST` | `/api/[controller]` | Authorize | CreateArtistResponseDto | `Task<IActionResult>` | `ArtistResponseController.cs::CreateArtistResponse` |
| `PUT` | `/api/[controller]/reject-tattoo-request/{tattooRequestId}` | Authorize | — | `Task<IActionResult>` | `ArtistResponseController.cs::RejectTattooRequest` |
| `POST` | `/api/[controller]` | Authorize | CreateArtistReviewDto | `Task<IActionResult>` | `ArtistReviewController.cs::CreateArtistReview` |
| `GET` | `/api/[controller]/artist/{tattooArtistId}` | Default | — | `Task<IActionResult>` | `ArtistReviewController.cs::GetArtistReviews` |
| `POST` | `/api/[controller]` | Authorize | CreateArtistUnavailableDateDto | `Task<IActionResult>` | `ArtistUnavailableDateController.cs::CreateUnavailableDate` |
| `GET` | `/api/[controller]/my-unavailable-dates` | Authorize | — | `Task<IActionResult>` | `ArtistUnavailableDateController.cs::GetMyUnavailableDates` |
| `DELETE` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ArtistUnavailableDateController.cs::DeleteUnavailableDate` |
| `POST` | `/api/[controller]/register` | Default | RegisterDto | `Task<IActionResult>` | `AuthController.cs::Register` |
| `POST` | `/api/[controller]/register/verify-code` | Default | VerifyRegisterCodeDto | `Task<IActionResult>` | `AuthController.cs::VerifyRegisterCode` |
| `POST` | `/api/[controller]/register/resend-code` | Default | ResendRegisterCodeDto | `Task<IActionResult>` | `AuthController.cs::ResendRegisterCode` |
| `POST` | `/api/[controller]/forgot-password/send-code` | Default | ForgotPasswordSendCodeDto | `Task<IActionResult>` | `AuthController.cs::SendForgotPasswordCode` |
| `POST` | `/api/[controller]/forgot-password/verify-code` | Default | VerifyPasswordResetCodeDto | `Task<IActionResult>` | `AuthController.cs::VerifyForgotPasswordCode` |
| `POST` | `/api/[controller]/forgot-password/reset` | Default | ResetPasswordWithCodeDto | `Task<IActionResult>` | `AuthController.cs::ResetPassword` |
| `POST` | `/api/[controller]/login` | Default | LoginDto | `Task<IActionResult>` | `AuthController.cs::Login` |
| `GET` | `/api/[controller]` | Authorize | — | `Task<IActionResult>` | `ClientController.cs::GetAllClients` |
| `GET` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ClientController.cs::GetClientById` |
| `POST` | `/api/[controller]/profile` | Authorize | CreateClientDto | `Task<IActionResult>` | `ClientController.cs::CreateClientProfile` |
| `PUT` | `/api/[controller]/profile` | Authorize | UpdateClientDto | `Task<IActionResult>` | `ClientController.cs::UpdateClientProfile` |
| `DELETE` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ClientController.cs::DeleteClient` |
| `POST` | `/api/[controller]/{studioId:int}` | Authorize | — | `Task<IActionResult>` | `ClientFavoriteStudioController.cs::Add` |
| `DELETE` | `/api/[controller]/{studioId:int}` | Authorize | — | `Task<IActionResult>` | `ClientFavoriteStudioController.cs::Remove` |
| `GET` | `/api/[controller]/my-favorites` | Authorize | — | `Task<IActionResult>` | `ClientFavoriteStudioController.cs::GetMine` |
| `GET` | `/api/[controller]` | Authorize | — | `Task<IActionResult>` | `ConsultationController.cs::GetAllConsultations` |
| `GET` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ConsultationController.cs::GetConsultationById` |
| `POST` | `/api/[controller]` | Authorize | CreateConsultationDto | `Task<IActionResult>` | `ConsultationController.cs::CreateConsultation` |
| `PUT` | `/api/[controller]/{id}` | Authorize | UpdateConsultationDto | `Task<IActionResult>` | `ConsultationController.cs::UpdateConsultation` |
| `DELETE` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `ConsultationController.cs::DeleteConsultation` |
| `PUT` | `/api/[controller]/complete-consultation/{tattooRequestId}` | Authorize | CompleteConsultationDto | `Task<IActionResult>` | `ConsultationController.cs::CompleteConsultation` |
| `PUT` | `/api/[controller]/reject-consultation/{tattooRequestId}` | Authorize | — | `Task<IActionResult>` | `ConsultationController.cs::RejectConsultation` |
| `GET` | `/api/legal/versions` | AllowAnonymous | — | `IActionResult` | `LegalController.cs::GetVersions` |
| `GET` | `/api/legal/consent-status` | Authorize | — | `Task<IActionResult>` | `LegalController.cs::GetConsentStatus` |
| `POST` | `/api/legal/consent` | Authorize | — | `Task<IActionResult>` | `LegalController.cs::AcceptCurrentVersions` |
| `GET` | `/api/mobile-billing/ai-context` | Authorize | — | `Task<IActionResult>` | `MobileBillingContextController.cs::Context` |
| `POST` | `/api/mobile-billing/verify` | Authorize | MobilePurchaseVerificationDto | `Task<IActionResult>` | `MobileBillingContextController.cs::Verify` |
| `POST` | `/api/subscription/webhooks/google` | AllowAnonymous | — | `Task<IActionResult>` | `MobileBillingWebhookController.cs::Google` |
| `POST` | `/api/subscription/webhooks/apple` | AllowAnonymous | AppleNotificationDto | `Task<IActionResult>` | `MobileBillingWebhookController.cs::Apple` |
| `GET` | `/api/media/private` | AllowAnonymous | — | `IActionResult` | `PrivateMediaController.cs::Read` |
| `GET` | `/api/media/public` | AllowAnonymous | — | `IActionResult` | `PrivateMediaController.cs::ReadPublic` |
| `GET` | `/api/[controller]/me` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::GetMyProfile` |
| `PATCH` | `/api/[controller]/user/first-name` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateFirstName` |
| `PATCH` | `/api/[controller]/user/last-name` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateLastName` |
| `PATCH` | `/api/[controller]/user/email` | Authorize | UpdateStringValueDto | `IActionResult` | `ProfileController.cs::UpdateEmail` |
| `POST` | `/api/[controller]/user/email/request-change` | Authorize | RequestEmailChangeDto | `Task<IActionResult>` | `ProfileController.cs::RequestEmailChange` |
| `POST` | `/api/[controller]/user/email/confirm-change` | Authorize | ConfirmEmailChangeDto | `Task<IActionResult>` | `ProfileController.cs::ConfirmEmailChange` |
| `POST` | `/api/[controller]/user/password/send-code` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::SendPasswordChangeCode` |
| `POST` | `/api/[controller]/user/password/change` | Authorize | ChangePasswordWithCodeDto | `Task<IActionResult>` | `ProfileController.cs::ChangePassword` |
| `PATCH` | `/api/[controller]/contact/profile-image` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::UpdateProfileImage` |
| `PATCH` | `/api/[controller]/contact/city` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateCity` |
| `PATCH` | `/api/[controller]/contact/country` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateCountry` |
| `PATCH` | `/api/[controller]/artist/description` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateDescription` |
| `PATCH` | `/api/[controller]/artist/specialty-styles` | Authorize | UpdateStringListDto | `Task<IActionResult>` | `ProfileController.cs::UpdateSpecialtyStyles` |
| `PATCH` | `/api/[controller]/artist/show-phone-number` | Authorize | UpdateBoolValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdatePhoneNumberVisibility` |
| `PATCH` | `/api/[controller]/consultation/duration` | Authorize | UpdateIntValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateConsultationDuration` |
| `PATCH` | `/api/[controller]/consultation/offers-online` | Authorize | UpdateBoolValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateOffersOnlineConsultation` |
| `PATCH` | `/api/[controller]/deposit/requires-deposit` | Authorize | UpdateBoolValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateRequiresDeposit` |
| `PATCH` | `/api/[controller]/deposit/amount` | Authorize | UpdateNullableDecimalValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateDepositAmount` |
| `POST` | `/api/[controller]/studio/requirements` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::AddRequirement` |
| `PATCH` | `/api/[controller]/studio/requirements/{id}` | Authorize | UpdateStringValueDto | `Task<IActionResult>` | `ProfileController.cs::UpdateRequirement` |
| `DELETE` | `/api/[controller]/studio/requirements/{id}` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::DeleteRequirement` |
| `POST` | `/api/[controller]/portfolio/images` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::AddPortfolioImage` |
| `DELETE` | `/api/[controller]/portfolio/images/{id}` | Authorize | — | `Task<IActionResult>` | `ProfileController.cs::DeletePortfolioImage` |
| `POST` | `/api/stripe/webhook` | Default | — | `Task<IActionResult>` | `StripeWebhookController.cs::Post` |
| `GET` | `/api/[controller]` | Default | — | `Task<IActionResult>` | `StudioController.cs::GetStudios` |
| `GET` | `/api/[controller]/{id:int}` | Default | — | `Task<IActionResult>` | `StudioController.cs::GetStudio` |
| `GET` | `/api/[controller]/join-search` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::SearchForJoin` |
| `GET` | `/api/[controller]/mine` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::GetMyStudio` |
| `POST` | `/api/[controller]/mine/create` | Authorize | CreateStudioDto | `Task<IActionResult>` | `StudioController.cs::CreateMyStudio` |
| `POST` | `/api/[controller]/{studioId:int}/join` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::RequestJoin` |
| `POST` | `/api/[controller]/join-requests/{requestId:int}/cancel` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::CancelPendingJoinRequest` |
| `POST` | `/api/[controller]/join-requests/{requestId:int}/accept` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::AcceptJoinRequest` |
| `POST` | `/api/[controller]/join-requests/{requestId:int}/reject` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::RejectJoinRequest` |
| `DELETE` | `/api/[controller]/members/{artistId:int}` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::RemoveMember` |
| `PATCH` | `/api/[controller]/open-for-join-requests` | Authorize | UpdateStudioOpenStateDto | `Task<IActionResult>` | `StudioController.cs::SetOpenForJoinRequests` |
| `PUT` | `/api/[controller]/mine` | Authorize | UpdateStudioDto | `Task<IActionResult>` | `StudioController.cs::UpdateMyStudio` |
| `POST` | `/api/[controller]/mine/cover` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::UpdateCover` |
| `POST` | `/api/[controller]/mine/logo` | Authorize | — | `Task<IActionResult>` | `StudioController.cs::UpdateLogo` |
| `GET` | `/api/subscription` | Authorize | — | `Task<IActionResult>` | `SubscriptionController.cs::Get` |
| `POST` | `/api/subscription/checkout` | Authorize | — | `Task<IActionResult>` | `SubscriptionController.cs::Checkout` |
| `POST` | `/api/subscription/portal` | Authorize | — | `Task<IActionResult>` | `SubscriptionController.cs::Portal` |
| `GET` | `/api/subscription/mobile-context` | Authorize | — | `Task<IActionResult>` | `SubscriptionController.cs::MobileContext` |
| `POST` | `/api/subscription/mobile/verify` | Authorize | MobilePurchaseVerificationDto | `Task<IActionResult>` | `SubscriptionController.cs::VerifyMobile` |
| `GET` | `/api/[controller]` | Default | — | `Task<IActionResult>` | `TattooArtistController.cs::GetAllTattooArtists` |
| `GET` | `/api/[controller]/{id:int}` | Default | — | `Task<IActionResult>` | `TattooArtistController.cs::GetTattooArtistById` |
| `GET` | `/api/[controller]/public/{slug}` | AllowAnonymous | — | `Task<IActionResult>` | `TattooArtistController.cs::GetPublicTattooArtist` |
| `GET` | `/api/[controller]/recommended` | Authorize | — | `Task<IActionResult>` | `TattooArtistController.cs::GetRecommendedTattooArtists` |
| `GET` | `/api/[controller]/search` | Default | — | `Task<IActionResult>` | `TattooArtistController.cs::SearchTattooArtists` |
| `POST` | `/api/[controller]/profile` | Authorize | CreateTattooArtistDto | `Task<IActionResult>` | `TattooArtistController.cs::CreateTattooArtistProfile` |
| `PUT` | `/api/[controller]/profile` | Authorize | UpdateArtistDto | `Task<IActionResult>` | `TattooArtistController.cs::UpdateTattooArtistProfile` |
| `DELETE` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `TattooArtistController.cs::DeleteTattooArtist` |
| `GET` | `/api/[controller]` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::GetAllTattooRequests` |
| `GET` | `/api/[controller]/my-artist-requests` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::GetMyArtistTattooRequests` |
| `GET` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::GetTattooRequestById` |
| `GET` | `/api/[controller]/my-requests` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::GetMyTattooRequests` |
| `POST` | `/api/[controller]` | Authorize | CreateTattooRequestDto | `Task<IActionResult>` | `TattooRequestController.cs::CreateTattooRequest` |
| `POST` | `/api/[controller]/with-images` | Authorize | CreateTattooRequestWithImagesDto | `Task<IActionResult>` | `TattooRequestController.cs::CreateTattooRequestWithImages` |
| `GET` | `/api/[controller]/{id}/availability` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::GetBookingAvailability` |
| `PUT` | `/api/[controller]/{id}` | Authorize | UpdateTattooRequestDto | `Task<IActionResult>` | `TattooRequestController.cs::UpdateTattooRequest` |
| `POST` | `/api/[controller]/{id:int}/under-review` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::MarkUnderReview` |
| `POST` | `/api/[controller]/{id:int}/cancel` | Authorize | CancelTattooRequestDto | `Task<IActionResult>` | `TattooRequestController.cs::Cancel` |
| `PUT` | `/api/[controller]/{id}/reject-by-artist` | Authorize | — | `Task<IActionResult>` | `TattooRequestController.cs::RejectTattooRequestByArtist` |
| `GET` | `/api/[controller]` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::GetAllTattooSessions` |
| `GET` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::GetTattooSessionById` |
| `POST` | `/api/[controller]` | Authorize | CreateTattooSessionDto | `Task<IActionResult>` | `TattooSessionController.cs::CreateTattooSession` |
| `PUT` | `/api/[controller]/{id}` | Authorize | UpdateTattooSessionDto | `Task<IActionResult>` | `TattooSessionController.cs::UpdateTattooSession` |
| `DELETE` | `/api/[controller]/{id}` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::DeleteTattooSession` |
| `PUT` | `/api/[controller]/add-more-sessions/{tattooRequestId}` | Authorize | AddAdditionalSessionsDto | `Task<IActionResult>` | `TattooSessionController.cs::AddMoreSessions` |
| `POST` | `/api/[controller]/start-tattoo/{tattooRequestId:int}` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::StartTattoo` |
| `PUT` | `/api/[controller]/complete-tattoo/{tattooRequestId}` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::CompleteTattoo` |
| `PUT` | `/api/[controller]/continue-tattoo/{tattooRequestId}` | Authorize | — | `Task<IActionResult>` | `TattooSessionController.cs::ContinueTattoo` |

## Backend-authoritative additions

- Paid AI draft creation is exposed at `POST /api/ai-tattoos/paid-draft`; entitlement remains project-bound.
- AI DTO mapping exposes `CanGenerate`, `CanEdit`, `NeedsPayment`, `HasGeneratedInitialVersion`, `EditingAccessUntil`.
- Legal versions and re-consent are backend-authoritative.
- Mobile billing context includes Google Play product/base-plan/trial-offer identifiers.
- Private managed media uses short-lived protected read tokens.
- Health endpoints: `GET /health/live`, `GET /health/ready`.