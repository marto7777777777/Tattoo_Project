using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Tattoo_Project.Data;
using Tattoo_Project.AI.Builders;
using Tattoo_Project.AI.Models;
using Tattoo_Project.AI.Planning;
using Tattoo_Project.DTOs.AiTattooDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;
using Stripe;
using Tattoo_Project.Security;

namespace Tattoo_Project.Services;

public class AiTattooService(
    TattooDbContext context,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IAiTattooPromptBuilder promptBuilder,
    IAiTattooPlanner tattooPlanner,
    UserManager<ApplicationUser> userManager,
    IPrivateMediaUrlService privateMedia,
    ILogger<AiTattooService> logger,
    IFileStorage storage,
    IImageSanitizer imageSanitizer,
    TimeProvider timeProvider) : IAiTattooService
{
    // The free entitlement is exactly one generated image. Refinements require
    // paid project access (admins remain unlimited for development/testing).
    private const int FreeEditLimit = 0;
    private readonly string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<ResultService<AiTattooProjectDto>> CreatePaidDraftAsync(CreateAiTattooProjectDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.TattooStyle) || string.IsNullOrWhiteSpace(dto.Placement) || string.IsNullOrWhiteSpace(dto.Description))
            return ResultService<AiTattooProjectDto>.Fail("Title, style, placement and description are required.");
        string? referenceUrl = null;
        if (dto.ReferenceImage != null)
        {
            var sanitized = await imageSanitizer.SanitizeAsync(dto.ReferenceImage, 10 * 1024 * 1024, 30_000_000);
            if (!sanitized.Success) return ResultService<AiTattooProjectDto>.Fail(sanitized.ErrorMessage!);
            referenceUrl = await storage.SaveAsync(sanitized.Data!.Bytes, "ai-tattoos", sanitized.Data.Extension, StoredFileVisibility.Private);
        }
        var project=new AiTattooProject{UserId=userId,Title=dto.Title.Trim(),TattooStyle=dto.TattooStyle.Trim(),Placement=dto.Placement.Trim(),InitialDescription=dto.Description.Trim(),InitialReferenceImageUrl=referenceUrl,IsFreeProject=false,FreeEditsUsed=FreeEditLimit,CreatedAt=UtcNow,UpdatedAt=UtcNow};
        context.AiTattooProjects.Add(project);
        try { await context.SaveChangesAsync(); }
        catch
        {
            if (referenceUrl != null) await storage.DeleteAsync(referenceUrl);
            throw;
        }
        var isAdmin = await IsAdminAsync(userId);
        return ResultService<AiTattooProjectDto>.Ok(Map(project, isAdmin));
    }

    public async Task<ResultService<AiTattooProjectDto>> GenerateInitialAsync(int id,string userId)
    {
        AiGenerationOperation operation;
        AiTattooProject projectSnapshot;
        string? referenceImage;
        bool isAdmin;

        await using (var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
        {
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AiProject:{id}","ai_operation_concurrency_conflict","Another AI operation is already running for this project.");
            var project=await context.AiTattooProjects.Include(x=>x.Versions).FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
            if(project==null)return ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.");
            isAdmin = await IsAdminAsync(userId);
            if(project.Versions.Count>0)return ResultService<AiTattooProjectDto>.Fail("This project already has an initial version.");
            if(project.ConsecutiveGenerationFailures>=AiGenerationRules.MaxConsecutiveFailures)
                throw new DomainConflictException("ai_generation_retry_limit_reached","AI generation stopped after three consecutive failed attempts.");
            if(!CanGenerate(project, isAdmin))return ResultService<AiTattooProjectDto>.Fail("Payment is required before generating this AI tattoo project.");
            if(project.ActiveOperationId!=null || await context.AiGenerationOperations.AnyAsync(x=>x.AiTattooProjectId==id&&x.Status==AiGenerationOperationStatuses.Generating))
                throw new DomainConflictException("ai_operation_in_progress","Another AI operation is already running for this project.");

            var now=UtcNow;
            var operationId=Guid.NewGuid();
            var epoch=checked(project.OperationEpoch+1);
            operation=new AiGenerationOperation
            {
                OperationId=operationId,AiTattooProjectId=id,UserId=userId,OperationType="initial",RequestKey=operationId.ToString("N"),OperationEpoch=epoch,
                Status=AiGenerationOperationStatuses.Generating,StartedAtUtc=now,LeaseExpiresAtUtc=now.Add(AiGenerationRules.LeaseDuration),CreatedAt=now,UpdatedAt=now
            };
            project.OperationEpoch=epoch;
            project.ActiveOperationId=operationId;
            context.AiGenerationOperations.Add(operation);
            referenceImage=project.InitialReferenceImageUrl;
            projectSnapshot=project;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        using var generationCts=new CancellationTokenSource(AiGenerationRules.HardTimeout);
        ResultService<string> result;
        try
        {
            var prompt=await BuildInitialPromptAsync(projectSnapshot,generationCts.Token);
            result=referenceImage==null
                ?await GenerateImageAsync(prompt,generationCts.Token)
                :await EditImageAsync(ToPhysicalPath(referenceImage),prompt,generationCts.Token);
        }
        catch(OperationCanceledException) when(generationCts.IsCancellationRequested)
        {
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_timeout",AiGenerationOperationStatuses.TimedOut);
            return ResultService<AiTattooProjectDto>.Fail("AI image generation timed out. Please try again.");
        }
        catch(Exception ex)
        {
            logger.LogWarning(ex,"AI initial generation failed for operation {OperationId}.",operation.OperationId);
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_failure",AiGenerationOperationStatuses.Failed);
            return ResultService<AiTattooProjectDto>.Fail("AI image generation is temporarily unavailable.");
        }
        if(!result.Success)
        {
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_failure",AiGenerationOperationStatuses.Failed);
            return ResultService<AiTattooProjectDto>.Fail(result.ErrorMessage!);
        }

        await using var finalize = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AiProject:{id}","ai_operation_concurrency_conflict","Another AI operation is updating this project.");
        context.ChangeTracker.Clear();
        var finalOperation=await context.AiGenerationOperations.FirstOrDefaultAsync(x=>x.OperationId==operation.OperationId);
        var finalProject=await context.AiTattooProjects.Include(x=>x.Versions).FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
        if(finalProject==null||finalOperation==null||finalOperation.Status!=AiGenerationOperationStatuses.Generating||!AiGenerationRules.OwnsFence(finalProject,operation.OperationId,operation.OperationEpoch))
        {
            await finalize.RollbackAsync();
            await DeleteStoredFileAsync(result.Data!);
            return ResultService<AiTattooProjectDto>.Fail("The AI operation is no longer valid.");
        }
        if(finalProject.Versions.Count>0)
        {
            finalOperation.Status=AiGenerationOperationStatuses.Completed;finalOperation.CompletedAtUtc=UtcNow;finalOperation.UpdatedAt=UtcNow;
            finalProject.ActiveOperationId=null;finalProject.ConsecutiveGenerationFailures=0;
            await context.SaveChangesAsync();await finalize.CommitAsync();await DeleteStoredFileAsync(result.Data!);
            return ResultService<AiTattooProjectDto>.Ok(Map(finalProject,isAdmin));
        }
        var version=new AiTattooVersion{VersionNumber=1,Prompt=finalProject.InitialDescription,ImageUrl=result.Data!,CreatedAt=UtcNow};
        finalProject.Versions.Add(version);finalProject.UpdatedAt=UtcNow;finalProject.ActiveOperationId=null;finalProject.ConsecutiveGenerationFailures=0;
        finalOperation.Status=AiGenerationOperationStatuses.Completed;finalOperation.CompletedAtUtc=UtcNow;finalOperation.UpdatedAt=UtcNow;
        try{await context.SaveChangesAsync();finalOperation.ResultVersionId=version.Id;await context.SaveChangesAsync();await finalize.CommitAsync();}
        catch{await finalize.RollbackAsync();await DeleteStoredFileAsync(result.Data!);throw;}
        return ResultService<AiTattooProjectDto>.Ok(Map(finalProject,isAdmin));
    }

    public async Task<ResultService<AiTattooProjectDto>> CreateProjectAsync(CreateAiTattooProjectDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.TattooStyle) || string.IsNullOrWhiteSpace(dto.Placement) || string.IsNullOrWhiteSpace(dto.Description))
            return ResultService<AiTattooProjectDto>.Fail("Title, style, placement and description are required.");

        var isAdmin = await IsAdminAsync(userId);
        if (!isAdmin)
        {
            var hasFreeProject = await context.AiTattooProjects.AnyAsync(x => x.UserId == userId && x.IsFreeProject);
            if (hasFreeProject)
                return ResultService<AiTattooProjectDto>.Fail("You have already used your one free AI tattoo project.");
        }

        string? referenceUrl = null;
        if (dto.ReferenceImage != null)
        {
            var sanitized = await imageSanitizer.SanitizeAsync(dto.ReferenceImage, 10 * 1024 * 1024, 30_000_000);
            if (!sanitized.Success) return ResultService<AiTattooProjectDto>.Fail(sanitized.ErrorMessage!);
            referenceUrl = await storage.SaveAsync(sanitized.Data!.Bytes, "ai-tattoos", sanitized.Data.Extension, StoredFileVisibility.Private);
        }

        var project = new AiTattooProject
        {
            UserId = userId, Title = dto.Title.Trim(), TattooStyle = dto.TattooStyle.Trim(), Placement = dto.Placement.Trim(),
            InitialDescription = dto.Description.Trim(), InitialReferenceImageUrl = referenceUrl, IsFreeProject = !isAdmin,
            FreeEditsUsed = 0, CreatedAt = UtcNow, UpdatedAt = UtcNow
        };
        context.AiTattooProjects.Add(project);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (!isAdmin && exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException && sqlException.Number is 2601 or 2627)
        {
            if (referenceUrl != null) await DeleteStoredFileAsync(referenceUrl);
            return ResultService<AiTattooProjectDto>.Fail("You have already used your one free AI tattoo project.");
        }
        catch
        {
            if(referenceUrl!=null) await DeleteStoredFileAsync(referenceUrl);
            throw;
        }

        // Initial free generation uses the exact same durable operation/fencing path as paid drafts.
        // A provider failure intentionally leaves the single free project present so it can be retried,
        // up to the project-level consecutive failure limit, without creating another free project.
        return await GenerateInitialAsync(project.Id,userId);
    }

    public async Task<ResultService<ICollection<AiTattooProjectDto>>> GetMyProjectsAsync(string userId)
    {
        var isAdmin = await IsAdminAsync(userId);
        var items = await context.AiTattooProjects.Include(x => x.Versions).Where(x => x.UserId == userId).OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return ResultService<ICollection<AiTattooProjectDto>>.Ok(items.Select(x => Map(x, isAdmin)).ToList());
    }

    public async Task<ResultService<AiTattooProjectDto>> GetProjectAsync(int id, string userId)
    {
        var isAdmin = await IsAdminAsync(userId);
        var project = await context.AiTattooProjects.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return project == null ? ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.") : ResultService<AiTattooProjectDto>.Ok(Map(project, isAdmin));
    }

    public async Task<ResultService<AiTattooProjectDto>> EditProjectAsync(int id, EditAiTattooProjectDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Instruction)) return ResultService<AiTattooProjectDto>.Fail("Edit instruction is required.");
        AiGenerationOperation operation;
        AiTattooProject projectSnapshot;
        string instruction;
        string sourcePath;
        bool isAdmin;

        await using (var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
        {
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AiProject:{id}","ai_operation_concurrency_conflict","Another AI operation is already running for this project.");
            var project = await context.AiTattooProjects.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (project == null) return ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.");
            isAdmin = await IsAdminAsync(userId);
            if(project.ConsecutiveGenerationFailures>=AiGenerationRules.MaxConsecutiveFailures)
                throw new DomainConflictException("ai_generation_retry_limit_reached","AI generation stopped after three consecutive failed attempts.");
            if (!CanEdit(project, isAdmin)) return ResultService<AiTattooProjectDto>.Fail("Payment is required to edit this AI tattoo project.");
            var source = dto.BaseVersionId.HasValue ? project.Versions.FirstOrDefault(x => x.Id == dto.BaseVersionId) : project.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (source == null) return ResultService<AiTattooProjectDto>.Fail("A source version was not found.");
            if(project.ActiveOperationId!=null || await context.AiGenerationOperations.AnyAsync(x=>x.AiTattooProjectId==id&&x.Status==AiGenerationOperationStatuses.Generating))
                throw new DomainConflictException("ai_operation_in_progress","Another AI operation is already running for this project.");

            instruction=dto.Instruction.Trim();
            var now=UtcNow;
            var operationId=Guid.NewGuid();
            var epoch=checked(project.OperationEpoch+1);
            operation=new AiGenerationOperation
            {
                OperationId=operationId,AiTattooProjectId=id,UserId=userId,OperationType="edit",RequestKey=operationId.ToString("N"),OperationEpoch=epoch,
                BaseVersionId=source.Id,Instruction=instruction,Status=AiGenerationOperationStatuses.Generating,
                StartedAtUtc=now,LeaseExpiresAtUtc=now.Add(AiGenerationRules.LeaseDuration),CreatedAt=now,UpdatedAt=now
            };
            project.OperationEpoch=epoch;project.ActiveOperationId=operationId;
            context.AiGenerationOperations.Add(operation);
            sourcePath=ToPhysicalPath(source.ImageUrl);
            projectSnapshot=project;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        using var generationCts=new CancellationTokenSource(AiGenerationRules.HardTimeout);
        ResultService<string> result;
        try
        {
            var prompt=await BuildEditPromptAsync(projectSnapshot,instruction,generationCts.Token);
            result=await EditImageAsync(sourcePath,prompt,generationCts.Token);
        }
        catch(OperationCanceledException) when(generationCts.IsCancellationRequested)
        {
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_timeout",AiGenerationOperationStatuses.TimedOut);
            return ResultService<AiTattooProjectDto>.Fail("AI image generation timed out. Please try again.");
        }
        catch(Exception ex)
        {
            logger.LogWarning(ex,"AI edit generation failed for operation {OperationId}.",operation.OperationId);
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_failure",AiGenerationOperationStatuses.Failed);
            return ResultService<AiTattooProjectDto>.Fail("AI image generation is temporarily unavailable.");
        }
        if (!result.Success)
        {
            await MarkAiOperationFailedAsync(id,operation.OperationId,operation.OperationEpoch,"provider_failure",AiGenerationOperationStatuses.Failed);
            return ResultService<AiTattooProjectDto>.Fail(result.ErrorMessage!);
        }

        await using var finalize = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AiProject:{id}","ai_operation_concurrency_conflict","Another AI operation is updating this project.");
        context.ChangeTracker.Clear();
        var finalOperation=await context.AiGenerationOperations.FirstOrDefaultAsync(x=>x.OperationId==operation.OperationId);
        var finalProject=await context.AiTattooProjects.Include(x=>x.Versions).FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
        if(finalProject==null||finalOperation==null||finalOperation.Status!=AiGenerationOperationStatuses.Generating||!AiGenerationRules.OwnsFence(finalProject,operation.OperationId,operation.OperationEpoch))
        {
            await finalize.RollbackAsync();await DeleteStoredFileAsync(result.Data!);return ResultService<AiTattooProjectDto>.Fail("The AI operation is no longer valid.");
        }
        var parent=finalProject.Versions.FirstOrDefault(x=>x.Id==finalOperation.BaseVersionId);
        if(parent==null){await finalize.RollbackAsync();await DeleteStoredFileAsync(result.Data!);return ResultService<AiTattooProjectDto>.Fail("The source version no longer exists.");}
        var nextVersion=finalProject.Versions.Count==0?1:finalProject.Versions.Max(x=>x.VersionNumber)+1;
        var version=new AiTattooVersion{VersionNumber=nextVersion,Prompt=finalOperation.Instruction!,ImageUrl=result.Data!,ParentVersionId=parent.Id,CreatedAt=UtcNow};
        finalProject.Versions.Add(version);finalProject.UpdatedAt=UtcNow;finalProject.ActiveOperationId=null;finalProject.ConsecutiveGenerationFailures=0;
        finalOperation.Status=AiGenerationOperationStatuses.Completed;finalOperation.CompletedAtUtc=UtcNow;finalOperation.UpdatedAt=UtcNow;
        try{await context.SaveChangesAsync();finalOperation.ResultVersionId=version.Id;await context.SaveChangesAsync();await finalize.CommitAsync();}
        catch{await finalize.RollbackAsync();await DeleteStoredFileAsync(result.Data!);throw;}
        return ResultService<AiTattooProjectDto>.Ok(Map(finalProject,isAdmin));
    }

    public async Task<ResultService<CheckoutSessionDto>> CreateCheckoutAsync(int projectId, string userId)
    {
        var secret = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) return ResultService<CheckoutSessionDto>.Fail("Stripe is not configured.");
        const long amount = 1249;
        const string currency = "eur";
        const string product = "ai_project_pass";
        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";

        // At most one retry of the outer loop is needed: it is used only when an
        // existing Stripe session is server-confirmed expired and a fresh durable
        // checkout intent may safely be created.
        for (var pass = 0; pass < 2; pass++)
        {
            AiProjectCheckoutAttempt attempt;
            await using (var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
            {
                await DatabaseApplicationLock.AcquireAsync(context, $"InkRoute:AiCheckout:{projectId}", "ai_checkout_concurrency_conflict", "The AI checkout is being updated by another request.");
                context.ChangeTracker.Clear();
                var project = await context.AiTattooProjects.FirstOrDefaultAsync(x => x.Id == projectId && x.UserId == userId);
                if (project == null) return ResultService<CheckoutSessionDto>.Fail("AI tattoo project was not found.");
                if (project.IsFreeProject) return ResultService<CheckoutSessionDto>.Fail("The free project cannot purchase editing access through this endpoint. Create a paid draft for another project.");

                attempt = await context.AiProjectCheckoutAttempts.FirstOrDefaultAsync(x =>
                    x.UserId == userId && x.AiTattooProjectId == projectId && x.Product == product &&
                    (x.Status == AiProjectCheckoutAttemptStatuses.Creating ||
                     x.Status == AiProjectCheckoutAttemptStatuses.SessionCreated ||
                     x.Status == AiProjectCheckoutAttemptStatuses.PaymentConfirmed ||
                     x.Status == AiProjectCheckoutAttemptStatuses.GrantPending));

                if (attempt == null)
                {
                    var id = Guid.NewGuid();
                    attempt = new AiProjectCheckoutAttempt
                    {
                        Id = id,
                        UserId = userId,
                        AiTattooProjectId = projectId,
                        Product = product,
                        ExpectedBaseAmountMinor = amount,
                        Currency = currency,
                        PriceSemantics = "pre_tax",
                        Status = AiProjectCheckoutAttemptStatuses.Creating,
                        StripeIdempotencyKey = $"inkroute-ai-checkout:{id:N}",
                        CreatedAtUtc = UtcNow,
                        UpdatedAtUtc = UtcNow
                    };
                    context.AiProjectCheckoutAttempts.Add(attempt);
                    await context.SaveChangesAsync();
                }

                if (attempt.Status is AiProjectCheckoutAttemptStatuses.PaymentConfirmed or AiProjectCheckoutAttemptStatuses.GrantPending)
                {
                    await tx.CommitAsync();
                    return ResultService<CheckoutSessionDto>.Fail("The AI Project Pass payment is already being processed.");
                }

                if (attempt.Status == AiProjectCheckoutAttemptStatuses.SessionCreated &&
                    !string.IsNullOrWhiteSpace(attempt.StripeCheckoutUrl) &&
                    attempt.ExpiresAtUtc is { } expires && expires > UtcNow.AddSeconds(30))
                {
                    var existingUrl = attempt.StripeCheckoutUrl;
                    await tx.CommitAsync();
                    return ResultService<CheckoutSessionDto>.Ok(new CheckoutSessionDto { Url = existingUrl! });
                }

                await tx.CommitAsync();
            }

            if (attempt.Status == AiProjectCheckoutAttemptStatuses.SessionCreated && !string.IsNullOrWhiteSpace(attempt.StripeCheckoutSessionId))
            {
                var verified = await RetrieveStripeCheckoutSessionAsync(secret, attempt.StripeCheckoutSessionId);
                if (!verified.Success) return ResultService<CheckoutSessionDto>.Fail("Stripe checkout state could not be verified.");

                if (string.Equals(verified.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(verified.Status, "complete", StringComparison.OrdinalIgnoreCase))
                    return ResultService<CheckoutSessionDto>.Fail("The AI Project Pass payment is already being processed.");

                if (!string.Equals(verified.Status, "expired", StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateCheckoutAttemptFromStripeAsync(attempt.Id, projectId, verified.Url, verified.ExpiresAtUtc);
                    if (!string.IsNullOrWhiteSpace(verified.Url))
                        return ResultService<CheckoutSessionDto>.Ok(new CheckoutSessionDto { Url = verified.Url! });
                    return ResultService<CheckoutSessionDto>.Fail("Stripe checkout is not currently available.");
                }

                await using var expireTx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                await DatabaseApplicationLock.AcquireAsync(context, $"InkRoute:AiCheckout:{projectId}", "ai_checkout_concurrency_conflict", "The AI checkout is being updated by another request.");
                context.ChangeTracker.Clear();
                var stale = await context.AiProjectCheckoutAttempts.FirstOrDefaultAsync(x => x.Id == attempt.Id);
                if (stale != null && stale.Status == AiProjectCheckoutAttemptStatuses.SessionCreated)
                {
                    AiProjectCheckoutAttemptRules.Transition(stale, AiProjectCheckoutAttemptStatuses.Expired, UtcNow);
                    await context.SaveChangesAsync();
                }
                await expireTx.CommitAsync();
                continue;
            }

            // A Creating attempt is durable before this HTTP call. Repeating this
            // call uses the same Stripe idempotency key and therefore recovers the
            // same Stripe operation after a timeout/crash instead of creating a
            // second uncontrolled Checkout Session.
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", attempt.StripeIdempotencyKey);
            request.Content = new FormUrlEncodedContent(new Dictionary<string,string>
            {
                ["mode"]="payment",
                ["success_url"]=$"{frontendUrl}/ai-studio/{projectId}?payment=success",
                ["cancel_url"]=$"{frontendUrl}/ai-studio/{projectId}?payment=cancelled",
                ["line_items[0][price_data][currency]"]=attempt.Currency,
                ["line_items[0][price_data][unit_amount]"]=attempt.ExpectedBaseAmountMinor.ToString(),
                ["line_items[0][price_data][tax_behavior]"]="exclusive",
                ["line_items[0][price_data][product_data][name]"]="InkRoute AI Project — 30 Day Pass",
                ["line_items[0][quantity]"]="1",
                ["automatic_tax[enabled]"]="true",
                ["billing_address_collection"]="required",
                ["metadata[purpose]"]=product,
                ["metadata[checkoutAttemptId]"]=attempt.Id.ToString("D"),
                ["metadata[projectId]"]=projectId.ToString(),
                ["metadata[amount_minor]"]=attempt.ExpectedBaseAmountMinor.ToString(),
                ["metadata[price_semantics]"]=attempt.PriceSemantics
            });

            HttpResponseMessage response;
            try { response = await httpClientFactory.CreateClient().SendAsync(request); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Stripe AI checkout creation failed for attempt {AttemptId}; the durable attempt remains retryable.", attempt.Id);
                return ResultService<CheckoutSessionDto>.Fail("Stripe checkout could not be created.");
            }
            using var responseDispose = response;
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Stripe AI checkout returned HTTP {StatusCode} for attempt {AttemptId}.", (int)response.StatusCode, attempt.Id);
                return ResultService<CheckoutSessionDto>.Fail("Stripe checkout could not be created.");
            }

            using var doc = JsonDocument.Parse(json);
            var sessionId = doc.RootElement.GetProperty("id").GetString();
            var url = doc.RootElement.GetProperty("url").GetString();
            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_at", out var expiresElement) && expiresElement.TryGetInt64(out var expiresUnix))
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix).UtcDateTime;
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(url))
                return ResultService<CheckoutSessionDto>.Fail("Stripe checkout returned an invalid session.");

            await using (var attachTx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
            {
                await DatabaseApplicationLock.AcquireAsync(context, $"InkRoute:AiCheckout:{projectId}", "ai_checkout_concurrency_conflict", "The AI checkout is being updated by another request.");
                context.ChangeTracker.Clear();
                var current = await context.AiProjectCheckoutAttempts.FirstOrDefaultAsync(x => x.Id == attempt.Id);
                if (current == null) return ResultService<CheckoutSessionDto>.Fail("The server checkout attempt no longer exists.");
                if (!string.IsNullOrWhiteSpace(current.StripeCheckoutSessionId) && current.StripeCheckoutSessionId != sessionId)
                    return ResultService<CheckoutSessionDto>.Fail("Stripe checkout reconciliation conflict.");
                if (current.Status == AiProjectCheckoutAttemptStatuses.Creating)
                    AiProjectCheckoutAttemptRules.Transition(current, AiProjectCheckoutAttemptStatuses.SessionCreated, UtcNow);
                current.StripeCheckoutSessionId = sessionId;
                current.StripeCheckoutUrl = url;
                current.ExpiresAtUtc = expiresAt;
                await context.SaveChangesAsync();
                await attachTx.CommitAsync();
            }
            return ResultService<CheckoutSessionDto>.Ok(new CheckoutSessionDto { Url = url! });
        }

        return ResultService<CheckoutSessionDto>.Fail("A new Stripe checkout could not be created after the previous session expired.");
    }

    public async Task<ResultService> ProcessVerifiedStripeEventAsync(Event stripeEvent)
    {
        if (stripeEvent.Type != "checkout.session.completed" || stripeEvent.Data.Object is not Stripe.Checkout.Session session)
            return ResultService.Ok();
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return ResultService.Fail("AI Project Pass payment is not confirmed as paid.");
        if (session.Metadata == null || !session.Metadata.TryGetValue("purpose", out var purpose) || purpose != "ai_project_pass")
            return ResultService.Fail("Stripe checkout purpose metadata is invalid.");

        AiProjectCheckoutAttempt? attempt = null;
        AiProjectPayment? legacyPayment = null;
        if (session.Metadata.TryGetValue("checkoutAttemptId", out var attemptRaw) && Guid.TryParse(attemptRaw, out var attemptId))
            attempt = await context.AiProjectCheckoutAttempts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt == null)
            legacyPayment = await context.AiProjectPayments.AsNoTracking().FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == session.Id);
        if (attempt == null && legacyPayment == null)
        {
            // A signed paid event with no server-created intent cannot be granted safely.
            // Acknowledge it to avoid an endless provider retry storm; the durable
            // ProviderWebhookEvent row plus this critical log preserves the event
            // identity for manual reconciliation. New checkouts cannot enter this
            // state because their attempt is committed before Stripe is called.
            logger.LogCritical("Paid Stripe AI checkout {SessionId} has no durable InkRoute checkout attempt/payment and requires manual reconciliation. Event {EventId}.", session.Id, stripeEvent.Id);
            return ResultService.Ok();
        }

        var projectIdForLock = attempt?.AiTattooProjectId ?? legacyPayment!.AiTattooProjectId;
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await DatabaseApplicationLock.AcquireAsync(context, $"InkRoute:AiPass:{projectIdForLock}", "ai_pass_concurrency_conflict", "The AI Project Pass is being updated by another request. Retry the webhook.");
        context.ChangeTracker.Clear();

        if (attempt != null)
        {
            attempt = await context.AiProjectCheckoutAttempts.Include(x => x.AiTattooProject).FirstOrDefaultAsync(x => x.Id == attempt.Id);
            if (attempt == null) return ResultService.Fail("Stripe checkout attempt no longer exists.");
            if (attempt.Status == AiProjectCheckoutAttemptStatuses.Granted) { await transaction.CommitAsync(); return ResultService.Ok(); }
            if (attempt.Product != "ai_project_pass") return ResultService.Fail("Stripe checkout product binding is invalid.");
            if (!session.Metadata.TryGetValue("projectId", out var metadataProjectId) || metadataProjectId != attempt.AiTattooProjectId.ToString())
                return ResultService.Fail("Stripe checkout project metadata is invalid.");
            if (!string.IsNullOrWhiteSpace(attempt.StripeCheckoutSessionId) && attempt.StripeCheckoutSessionId != session.Id)
                return ResultService.Fail("Stripe checkout session binding is invalid.");
            if (session.AmountSubtotal != attempt.ExpectedBaseAmountMinor || !string.Equals(session.Currency, attempt.Currency, StringComparison.OrdinalIgnoreCase))
                return ResultService.Fail("Stripe checkout base amount or currency does not match the immutable server checkout snapshot.");
            if (attempt.PriceSemantics != "pre_tax")
                return ResultService.Fail("Stripe checkout price semantics are invalid.");

            attempt.StripeCheckoutSessionId ??= session.Id;
            if (attempt.Status == AiProjectCheckoutAttemptStatuses.Creating)
                AiProjectCheckoutAttemptRules.Transition(attempt, AiProjectCheckoutAttemptStatuses.SessionCreated, UtcNow);
            if (attempt.Status == AiProjectCheckoutAttemptStatuses.SessionCreated)
                AiProjectCheckoutAttemptRules.Transition(attempt, AiProjectCheckoutAttemptStatuses.PaymentConfirmed, UtcNow);
            if (attempt.Status == AiProjectCheckoutAttemptStatuses.PaymentConfirmed)
                AiProjectCheckoutAttemptRules.Transition(attempt, AiProjectCheckoutAttemptStatuses.GrantPending, UtcNow);

            var payment = await context.AiProjectPayments.Include(x => x.AiTattooProject).FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == session.Id);
            if (payment == null)
            {
                payment = new AiProjectPayment
                {
                    UserId = attempt.UserId,
                    AiTattooProjectId = attempt.AiTattooProjectId,
                    StripeCheckoutSessionId = session.Id,
                    AmountInMinorUnits = attempt.ExpectedBaseAmountMinor,
                    Currency = attempt.Currency,
                    Status = "pending",
                    CreatedAt = attempt.CreatedAtUtc
                };
                context.AiProjectPayments.Add(payment);
            }
            if (payment.Status != "paid")
            {
                var now = UtcNow;
                var until = AiPassGrantRules.CalculateEnd(now, attempt.AiTattooProject.EditingAccessUntil);
                payment.Status = "paid";
                payment.PaidAt = now;
                payment.StripePaymentIntentId = session.PaymentIntentId;
                payment.AccessGrantedUntil = until;
                attempt.AiTattooProject.EditingAccessUntil = until;
                attempt.AiTattooProject.UpdatedAt = now;
                AiProjectCheckoutAttemptRules.Transition(attempt, AiProjectCheckoutAttemptStatuses.Granted, now);
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        // Backward-compatible reconciliation for checkout sessions created before
        // durable checkout attempts existed. These rows already contain the server
        // price snapshot and are still protected by the unique Stripe session index.
        var oldPayment = await context.AiProjectPayments.Include(x => x.AiTattooProject).FirstAsync(x => x.StripeCheckoutSessionId == session.Id);
        if (oldPayment.Status == "paid") { await transaction.CommitAsync(); return ResultService.Ok(); }
        if (!session.Metadata.TryGetValue("projectId", out var oldProjectId) || oldProjectId != oldPayment.AiTattooProjectId.ToString())
            return ResultService.Fail("Stripe checkout project metadata is invalid.");
        if (session.AmountSubtotal != oldPayment.AmountInMinorUnits || !string.Equals(session.Currency, oldPayment.Currency, StringComparison.OrdinalIgnoreCase))
            return ResultService.Fail("Stripe checkout base amount or currency does not match the server-created payment.");
        var legacyNow = UtcNow;
        var legacyUntil = AiPassGrantRules.CalculateEnd(legacyNow, oldPayment.AiTattooProject.EditingAccessUntil);
        oldPayment.Status = "paid";
        oldPayment.PaidAt = legacyNow;
        oldPayment.StripePaymentIntentId = session.PaymentIntentId;
        oldPayment.AccessGrantedUntil = legacyUntil;
        oldPayment.AiTattooProject.EditingAccessUntil = legacyUntil;
        oldPayment.AiTattooProject.UpdatedAt = legacyNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return ResultService.Ok();
    }

    private async Task UpdateCheckoutAttemptFromStripeAsync(Guid attemptId, int projectId, string? url, DateTime? expiresAtUtc)
    {
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await DatabaseApplicationLock.AcquireAsync(context, $"InkRoute:AiCheckout:{projectId}", "ai_checkout_concurrency_conflict", "The AI checkout is being updated by another request.");
        context.ChangeTracker.Clear();
        var current = await context.AiProjectCheckoutAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (current != null && current.Status == AiProjectCheckoutAttemptStatuses.SessionCreated)
        {
            if (!string.IsNullOrWhiteSpace(url)) current.StripeCheckoutUrl = url;
            current.ExpiresAtUtc = expiresAtUtc ?? current.ExpiresAtUtc;
            current.UpdatedAtUtc = UtcNow;
            await context.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }

    private async Task<(bool Success, string? Status, string? PaymentStatus, string? Url, DateTime? ExpiresAtUtc)> RetrieveStripeCheckoutSessionAsync(string secret, string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/checkout/sessions/{Uri.EscapeDataString(sessionId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        HttpResponseMessage response;
        try { response = await httpClientFactory.CreateClient().SendAsync(request); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stripe checkout state retrieval failed for session {SessionId}.", sessionId);
            return (false, null, null, null, null);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode) return (false, null, null, null, null);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var statusNode) ? statusNode.GetString() : null;
            var paymentStatus = root.TryGetProperty("payment_status", out var paymentNode) ? paymentNode.GetString() : null;
            var url = root.TryGetProperty("url", out var urlNode) ? urlNode.GetString() : null;
            DateTime? expires = null;
            if (root.TryGetProperty("expires_at", out var expiresNode) && expiresNode.TryGetInt64(out var unix))
                expires = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            return (true, status, paymentStatus, url, expires);
        }
    }

    private async Task MarkAiOperationFailedAsync(int projectId,Guid operationId,long operationEpoch,string failureCode,string finalStatus)
    {
        try
        {
            context.ChangeTracker.Clear();
            await using var tx=await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await DatabaseApplicationLock.AcquireAsync(context,$"InkRoute:AiProject:{projectId}","ai_operation_concurrency_conflict","The AI project is being updated by another request.");
            var operation=await context.AiGenerationOperations.FirstOrDefaultAsync(x=>x.OperationId==operationId);
            var project=await context.AiTattooProjects.FirstOrDefaultAsync(x=>x.Id==projectId);
            if(operation!=null&&project!=null&&operation.Status==AiGenerationOperationStatuses.Generating&&AiGenerationRules.OwnsFence(project,operationId,operationEpoch))
            {
                var now=UtcNow;
                operation.Status=finalStatus;operation.FailureCode=failureCode;operation.UpdatedAt=now;operation.CompletedAtUtc=now;
                project.ActiveOperationId=null;
                project.ConsecutiveGenerationFailures=Math.Min(AiGenerationRules.MaxConsecutiveFailures,project.ConsecutiveGenerationFailures+1);
                project.UpdatedAt=now;
                await context.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }
        catch(Exception ex){logger.LogWarning(ex,"Failed to finalize failed AI operation {OperationId}.",operationId);}
    }

    private async Task DeleteStoredFileAsync(string storedPath)
    {
        try
        {
            await storage.DeleteAsync(storedPath);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove AI media file during compensation; queuing durable cleanup.");
            var now=UtcNow;
            context.FileCleanupTasks.Add(new FileCleanupTask
            {
                StorageKey=storedPath,Reason="ai_generation_compensation",RetryCount=0,CreatedAt=now,UpdatedAt=now,LastError=ex.GetType().Name
            });
            try { await context.SaveChangesAsync(); }
            catch(Exception persistenceError)
            {
                logger.LogCritical(persistenceError,"Failed to persist AI media cleanup task after generation compensation failure.");
            }
        }
    }

    private async Task<bool> IsAdminAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user != null && await userManager.IsInRoleAsync(user, UserRoles.Admin);
    }

    private bool CanGenerate(AiTattooProject p, bool isAdmin)
        => p.Versions.Count == 0 && p.ConsecutiveGenerationFailures < AiGenerationRules.MaxConsecutiveFailures && (isAdmin || p.IsFreeProject || p.EditingAccessUntil > UtcNow);

    private bool CanEdit(AiTattooProject p, bool isAdmin)
        => p.Versions.Count > 0 && p.ConsecutiveGenerationFailures < AiGenerationRules.MaxConsecutiveFailures && (isAdmin || p.EditingAccessUntil > UtcNow);

    private AiTattooProjectDto Map(AiTattooProject p, bool isAdmin) => new()
    {
        Id = p.Id,
        Title = p.Title,
        TattooStyle = p.TattooStyle,
        Placement = p.Placement,
        InitialDescription = p.InitialDescription,
        IsFreeProject = p.IsFreeProject,
        FreeEditsUsed = p.FreeEditsUsed,
        FreeEditsRemaining = isAdmin ? int.MaxValue : Math.Max(0, FreeEditLimit - p.FreeEditsUsed),
        EditingAccessUntil = p.EditingAccessUntil,
        HasGeneratedInitialVersion = p.Versions.Count > 0,
        CanGenerate = CanGenerate(p, isAdmin),
        CanEdit = CanEdit(p, isAdmin),
        NeedsPayment = !isAdmin && !p.IsFreeProject && !(p.EditingAccessUntil > UtcNow),
        CreatedAt = p.CreatedAt,
        Versions = p.Versions.OrderBy(x => x.VersionNumber).Select(x => new AiTattooVersionDto
        {
            Id = x.Id,
            VersionNumber = x.VersionNumber,
            Prompt = x.Prompt,
            ImageUrl = privateMedia.CreateReadUrl(x.ImageUrl),
            CreatedAt = x.CreatedAt
        }).ToList()
    };
    private async Task<string> BuildInitialPromptAsync(AiTattooProject project, CancellationToken cancellationToken = default)
    {
        var planningContext = await promptBuilder.BuildGenerationPromptAsync(
            new AiTattooPromptContext
            {
                TattooStyle = project.TattooStyle,
                Placement = project.Placement,
                ClientDescription = project.InitialDescription,
                HasReferenceImage = !string.IsNullOrWhiteSpace(project.InitialReferenceImageUrl)
            });

        return await tattooPlanner.CreateGenerationPromptAsync(planningContext, cancellationToken);
    }

    private async Task<string> BuildEditPromptAsync(
        AiTattooProject project,
        string instruction, CancellationToken cancellationToken = default)
    {
        var planningContext = await promptBuilder.BuildEditPromptAsync(
            new AiTattooEditContext
            {
                TattooStyle = project.TattooStyle,
                Placement = project.Placement,
                InitialDescription = project.InitialDescription,
                EditInstruction = instruction
            });

        return await tattooPlanner.CreateEditPromptAsync(planningContext, cancellationToken);
    }

    private async Task<ResultService<string>> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var key=configuration["OpenAI:ApiKey"]; if(string.IsNullOrWhiteSpace(key)) return ResultService<string>.Fail("OpenAI is not configured.");
        using var req=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/images/generations"); req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        req.Content=new StringContent(JsonSerializer.Serialize(new { model=configuration["OpenAI:ImageModel"]??"gpt-image-2", prompt, size="1024x1024", quality="medium", output_format="png"}),Encoding.UTF8,"application/json");
        return await SendOpenAiImageRequest(req,cancellationToken);
    }
    private async Task<ResultService<string>> EditImageAsync(string physicalPath,string prompt, CancellationToken cancellationToken = default)
    {
        var key=configuration["OpenAI:ApiKey"]; if(string.IsNullOrWhiteSpace(key)) return ResultService<string>.Fail("OpenAI is not configured."); if(!System.IO.File.Exists(physicalPath)) return ResultService<string>.Fail("Source image file was not found.");
        using var req=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/images/edits"); req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        using var form=new MultipartFormDataContent(); form.Add(new StringContent(configuration["OpenAI:ImageModel"]??"gpt-image-2"),"model"); form.Add(new StringContent(prompt),"prompt"); form.Add(new StringContent("1024x1024"),"size"); form.Add(new StringContent("medium"),"quality"); form.Add(new StringContent("png"),"output_format");
        var bytes=await System.IO.File.ReadAllBytesAsync(physicalPath,cancellationToken); var image=new ByteArrayContent(bytes); image.Headers.ContentType=new MediaTypeHeaderValue("image/png"); form.Add(image,"image",Path.GetFileName(physicalPath)); req.Content=form;
        return await SendOpenAiImageRequest(req,cancellationToken);
    }
    private async Task<ResultService<string>> SendOpenAiImageRequest(HttpRequestMessage req, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = Timeout.InfiniteTimeSpan;
        using var response = await client.SendAsync(req,cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI image request failed with HTTP {StatusCode}.", (int)response.StatusCode);
            return ResultService<string>.Fail("AI image generation is temporarily unavailable.");
        }
        using var doc=JsonDocument.Parse(body); var b64=doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString(); if(string.IsNullOrWhiteSpace(b64)) return ResultService<string>.Fail("The AI did not return an image.");
        byte[] imageBytes;
        try { imageBytes = Convert.FromBase64String(b64); }
        catch (FormatException) { return ResultService<string>.Fail("The AI returned invalid image data."); }
        var key = await storage.SaveAsync(imageBytes, "ai-tattoos", ".png", StoredFileVisibility.Private);
        return ResultService<string>.Ok(key);
    }
    private string ToPhysicalPath(string storedPath)
    {
        if (storage.TryResolvePath(storedPath, out var managedPath, out _, out _)) return managedPath;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var root = Path.GetFullPath(webRoot) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(root, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid legacy media path.");
        return candidate;
    }
}
