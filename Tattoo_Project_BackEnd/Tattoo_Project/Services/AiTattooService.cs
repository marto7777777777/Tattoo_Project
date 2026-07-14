using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.AI.Builders;
using Tattoo_Project.AI.Models;
using Tattoo_Project.DTOs.AiTattooDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services;

public class AiTattooService(TattooDbContext context, IWebHostEnvironment environment, IConfiguration configuration, IHttpClientFactory httpClientFactory, IAiTattooPromptBuilder promptBuilder) : IAiTattooService
{
    private const int FreeEditLimit = 2;
    private readonly string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<ResultService<AiTattooProjectDto>> CreatePaidDraftAsync(CreateAiTattooProjectDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.TattooStyle) || string.IsNullOrWhiteSpace(dto.Placement) || string.IsNullOrWhiteSpace(dto.Description))
            return ResultService<AiTattooProjectDto>.Fail("Title, style, placement and description are required.");
        string? referenceUrl=null;
        if(dto.ReferenceImage!=null){var v=ValidateImage(dto.ReferenceImage);if(!v.Success)return ResultService<AiTattooProjectDto>.Fail(v.ErrorMessage!);referenceUrl=await SaveUploadedImageAsync(dto.ReferenceImage);}
        var project=new AiTattooProject{UserId=userId,Title=dto.Title.Trim(),TattooStyle=dto.TattooStyle.Trim(),Placement=dto.Placement.Trim(),InitialDescription=dto.Description.Trim(),InitialReferenceImageUrl=referenceUrl,IsFreeProject=false,FreeEditsUsed=FreeEditLimit,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
        context.AiTattooProjects.Add(project);await context.SaveChangesAsync();return ResultService<AiTattooProjectDto>.Ok(Map(project));
    }

    public async Task<ResultService<AiTattooProjectDto>> GenerateInitialAsync(int id,string userId)
    {
        var project=await context.AiTattooProjects.Include(x=>x.Versions).FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==userId);
        if(project==null)return ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.");
        if(project.Versions.Count>0)return ResultService<AiTattooProjectDto>.Fail("This project already has an initial version.");
        if(!CanEdit(project))return ResultService<AiTattooProjectDto>.Fail("Unlock this project before generating its first version.");
        var result=project.InitialReferenceImageUrl==null?await GenerateImageAsync(await BuildInitialPromptAsync(project)):await EditImageAsync(ToPhysicalPath(project.InitialReferenceImageUrl),await BuildInitialPromptAsync(project));
        if(!result.Success)return ResultService<AiTattooProjectDto>.Fail(result.ErrorMessage!);
        project.Versions.Add(new AiTattooVersion{VersionNumber=1,Prompt=project.InitialDescription,ImageUrl=result.Data!,CreatedAt=DateTime.UtcNow});project.UpdatedAt=DateTime.UtcNow;await context.SaveChangesAsync();return ResultService<AiTattooProjectDto>.Ok(Map(project));
    }

    public async Task<ResultService<AiTattooProjectDto>> CreateProjectAsync(CreateAiTattooProjectDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.TattooStyle) || string.IsNullOrWhiteSpace(dto.Placement) || string.IsNullOrWhiteSpace(dto.Description))
            return ResultService<AiTattooProjectDto>.Fail("Title, style, placement and description are required.");

        var hasFreeProject = await context.AiTattooProjects.AnyAsync(x => x.UserId == userId && x.IsFreeProject);
        if (hasFreeProject)
            return ResultService<AiTattooProjectDto>.Fail("Your free AI project has already been used. Create a paid project from the AI Studio.");

        string? referenceUrl = null;
        if (dto.ReferenceImage != null)
        {
            var validation = ValidateImage(dto.ReferenceImage);
            if (!validation.Success) return ResultService<AiTattooProjectDto>.Fail(validation.ErrorMessage!);
            referenceUrl = await SaveUploadedImageAsync(dto.ReferenceImage);
        }

        var project = new AiTattooProject
        {
            UserId = userId,
            Title = dto.Title.Trim(),
            TattooStyle = dto.TattooStyle.Trim(),
            Placement = dto.Placement.Trim(),
            InitialDescription = dto.Description.Trim(),
            InitialReferenceImageUrl = referenceUrl,
            IsFreeProject = true,
            FreeEditsUsed = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.AiTattooProjects.Add(project);
        await context.SaveChangesAsync();

        var generation = referenceUrl == null
            ? await GenerateImageAsync(await BuildInitialPromptAsync(project))
            : await EditImageAsync(ToPhysicalPath(referenceUrl), await BuildInitialPromptAsync(project));
        if (!generation.Success)
        {
            context.AiTattooProjects.Remove(project);
            await context.SaveChangesAsync();
            return ResultService<AiTattooProjectDto>.Fail(generation.ErrorMessage!);
        }

        project.Versions.Add(new AiTattooVersion { VersionNumber = 1, Prompt = project.InitialDescription, ImageUrl = generation.Data!, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        return ResultService<AiTattooProjectDto>.Ok(Map(project));
    }

    public async Task<ResultService<ICollection<AiTattooProjectDto>>> GetMyProjectsAsync(string userId)
    {
        var items = await context.AiTattooProjects.Include(x => x.Versions).Where(x => x.UserId == userId).OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return ResultService<ICollection<AiTattooProjectDto>>.Ok(items.Select(Map).ToList());
    }

    public async Task<ResultService<AiTattooProjectDto>> GetProjectAsync(int id, string userId)
    {
        var project = await context.AiTattooProjects.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return project == null ? ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.") : ResultService<AiTattooProjectDto>.Ok(Map(project));
    }

    public async Task<ResultService<AiTattooProjectDto>> EditProjectAsync(int id, EditAiTattooProjectDto dto, string userId)
    {
        var project = await context.AiTattooProjects.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (project == null) return ResultService<AiTattooProjectDto>.Fail("AI tattoo project was not found.");
        if (string.IsNullOrWhiteSpace(dto.Instruction)) return ResultService<AiTattooProjectDto>.Fail("Edit instruction is required.");
        if (!CanEdit(project)) return ResultService<AiTattooProjectDto>.Fail("This project is paused. Unlock it for 30 days to continue editing.");

        var source = dto.BaseVersionId.HasValue ? project.Versions.FirstOrDefault(x => x.Id == dto.BaseVersionId) : project.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        if (source == null) return ResultService<AiTattooProjectDto>.Fail("A source version was not found.");

        var prompt = await BuildEditPromptAsync(project, dto.Instruction.Trim());
        var result = await EditImageAsync(ToPhysicalPath(source.ImageUrl), prompt);
        if (!result.Success) return ResultService<AiTattooProjectDto>.Fail(result.ErrorMessage!);

        project.Versions.Add(new AiTattooVersion { VersionNumber = project.Versions.Max(x => x.VersionNumber) + 1, Prompt = dto.Instruction.Trim(), ImageUrl = result.Data!, ParentVersionId = source.Id, CreatedAt = DateTime.UtcNow });
        if (!(project.EditingAccessUntil > DateTime.UtcNow)) project.FreeEditsUsed++;
        project.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ResultService<AiTattooProjectDto>.Ok(Map(project));
    }

    public async Task<ResultService<CheckoutSessionDto>> CreateCheckoutAsync(int projectId, string userId)
    {
        var project = await context.AiTattooProjects.FirstOrDefaultAsync(x => x.Id == projectId && x.UserId == userId);
        if (project == null) return ResultService<CheckoutSessionDto>.Fail("AI tattoo project was not found.");

        var secret = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) return ResultService<CheckoutSessionDto>.Fail("Stripe is not configured.");
        var amount = configuration.GetValue<long?>("Stripe:AiProjectPassAmount") ?? 499;
        var currency = configuration["Stripe:Currency"] ?? "eur";
        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        request.Content = new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["mode"]="payment", ["success_url"]=$"{frontendUrl}/ai-studio/{project.Id}?payment=success", ["cancel_url"]=$"{frontendUrl}/ai-studio/{project.Id}?payment=cancelled",
            ["line_items[0][price_data][currency]"]=currency, ["line_items[0][price_data][unit_amount]"]=amount.ToString(), ["line_items[0][price_data][product_data][name]"]="InkRoute AI Project — 30 Day Pass",
            ["line_items[0][quantity]"]="1", ["metadata[projectId]"]=project.Id.ToString(), ["metadata[userId]"]=userId
        });
        var response = await httpClientFactory.CreateClient().SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return ResultService<CheckoutSessionDto>.Fail("Stripe checkout could not be created: " + json);
        using var doc = JsonDocument.Parse(json);
        var sessionId = doc.RootElement.GetProperty("id").GetString()!;
        var url = doc.RootElement.GetProperty("url").GetString()!;
        context.AiProjectPayments.Add(new AiProjectPayment { UserId=userId, AiTattooProjectId=project.Id, StripeCheckoutSessionId=sessionId, AmountInMinorUnits=amount, Currency=currency, Status="pending", CreatedAt=DateTime.UtcNow });
        await context.SaveChangesAsync();
        return ResultService<CheckoutSessionDto>.Ok(new CheckoutSessionDto { Url=url });
    }

    public async Task<ResultService> ProcessStripeWebhookAsync(string payload, string signature)
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret) || !VerifyStripeSignature(payload, signature, webhookSecret)) return ResultService.Fail("Invalid Stripe signature.");
        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.GetProperty("type").GetString() != "checkout.session.completed") return ResultService.Ok();
        var session = doc.RootElement.GetProperty("data").GetProperty("object");
        var sessionId = session.GetProperty("id").GetString()!;
        var payment = await context.AiProjectPayments.Include(x=>x.AiTattooProject).FirstOrDefaultAsync(x=>x.StripeCheckoutSessionId==sessionId);
        if (payment == null || payment.Status == "paid") return ResultService.Ok();
        payment.Status="paid"; payment.PaidAt=DateTime.UtcNow; payment.StripePaymentIntentId=session.TryGetProperty("payment_intent",out var pi)?pi.GetString():null;
        var startsAt = payment.AiTattooProject.EditingAccessUntil > DateTime.UtcNow ? payment.AiTattooProject.EditingAccessUntil.Value : DateTime.UtcNow;
        payment.AccessGrantedUntil=startsAt.AddDays(30); payment.AiTattooProject.EditingAccessUntil=payment.AccessGrantedUntil; payment.AiTattooProject.UpdatedAt=DateTime.UtcNow;
        await context.SaveChangesAsync(); return ResultService.Ok();
    }

    private bool CanEdit(AiTattooProject p) => p.EditingAccessUntil > DateTime.UtcNow || (p.IsFreeProject && p.FreeEditsUsed < FreeEditLimit);
    private AiTattooProjectDto Map(AiTattooProject p) => new() { Id=p.Id,Title=p.Title,TattooStyle=p.TattooStyle,Placement=p.Placement,InitialDescription=p.InitialDescription,IsFreeProject=p.IsFreeProject,FreeEditsUsed=p.FreeEditsUsed,FreeEditsRemaining=Math.Max(0,FreeEditLimit-p.FreeEditsUsed),EditingAccessUntil=p.EditingAccessUntil,CanEdit=CanEdit(p),NeedsPayment=!CanEdit(p),CreatedAt=p.CreatedAt,Versions=p.Versions.OrderBy(x=>x.VersionNumber).Select(x=>new AiTattooVersionDto{Id=x.Id,VersionNumber=x.VersionNumber,Prompt=x.Prompt,ImageUrl=x.ImageUrl,CreatedAt=x.CreatedAt}).ToList() };
    private Task<string> BuildInitialPromptAsync(AiTattooProject project) =>
        promptBuilder.BuildGenerationPromptAsync(new AiTattooPromptContext
        {
            TattooStyle = project.TattooStyle,
            Placement = project.Placement,
            ClientDescription = project.InitialDescription,
            HasReferenceImage = !string.IsNullOrWhiteSpace(project.InitialReferenceImageUrl)
        });

    private Task<string> BuildEditPromptAsync(AiTattooProject project, string instruction) =>
        promptBuilder.BuildEditPromptAsync(new AiTattooEditContext
        {
            TattooStyle = project.TattooStyle,
            Placement = project.Placement,
            InitialDescription = project.InitialDescription,
            EditInstruction = instruction
        });

    private async Task<ResultService<string>> GenerateImageAsync(string prompt)
    {
        var key=configuration["OpenAI:ApiKey"]; if(string.IsNullOrWhiteSpace(key)) return ResultService<string>.Fail("OpenAI is not configured.");
        using var req=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/images/generations"); req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        req.Content=new StringContent(JsonSerializer.Serialize(new { model=configuration["OpenAI:ImageModel"]??"gpt-image-1.5", prompt, size="1024x1024", quality="medium", output_format="png"}),Encoding.UTF8,"application/json");
        return await SendOpenAiImageRequest(req);
    }
    private async Task<ResultService<string>> EditImageAsync(string physicalPath,string prompt)
    {
        var key=configuration["OpenAI:ApiKey"]; if(string.IsNullOrWhiteSpace(key)) return ResultService<string>.Fail("OpenAI is not configured."); if(!File.Exists(physicalPath)) return ResultService<string>.Fail("Source image file was not found.");
        using var req=new HttpRequestMessage(HttpMethod.Post,"https://api.openai.com/v1/images/edits"); req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        using var form=new MultipartFormDataContent(); form.Add(new StringContent(configuration["OpenAI:ImageModel"]??"gpt-image-1.5"),"model"); form.Add(new StringContent(prompt),"prompt"); form.Add(new StringContent("1024x1024"),"size"); form.Add(new StringContent("medium"),"quality"); form.Add(new StringContent("png"),"output_format");
        var bytes=await File.ReadAllBytesAsync(physicalPath); var image=new ByteArrayContent(bytes); image.Headers.ContentType=new MediaTypeHeaderValue("image/png"); form.Add(image,"image",Path.GetFileName(physicalPath)); req.Content=form;
        return await SendOpenAiImageRequest(req);
    }
    private async Task<ResultService<string>> SendOpenAiImageRequest(HttpRequestMessage req)
    {
        var response=await httpClientFactory.CreateClient().SendAsync(req); var body=await response.Content.ReadAsStringAsync(); if(!response.IsSuccessStatusCode) return ResultService<string>.Fail("AI image request failed: "+body);
        using var doc=JsonDocument.Parse(body); var b64=doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString(); if(string.IsNullOrWhiteSpace(b64)) return ResultService<string>.Fail("The AI did not return an image.");
        var folder=Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),"uploads","ai-tattoos"); Directory.CreateDirectory(folder); var name=$"{Guid.NewGuid():N}.png"; await File.WriteAllBytesAsync(Path.Combine(folder,name),Convert.FromBase64String(b64)); return ResultService<string>.Ok($"/uploads/ai-tattoos/{name}");
    }
    private ResultService ValidateImage(Microsoft.AspNetCore.Http.IFormFile f){ if(f.Length==0||f.Length>10*1024*1024)return ResultService.Fail("Reference image must be between 1 byte and 10 MB."); if(!allowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))return ResultService.Fail("Only JPG, PNG and WebP images are allowed."); return ResultService.Ok(); }
    private async Task<string> SaveUploadedImageAsync(Microsoft.AspNetCore.Http.IFormFile f){var folder=Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),"uploads","ai-tattoos");Directory.CreateDirectory(folder);var name=$"ref-{Guid.NewGuid():N}{Path.GetExtension(f.FileName).ToLowerInvariant()}";await using var stream=File.Create(Path.Combine(folder,name));await f.CopyToAsync(stream);return $"/uploads/ai-tattoos/{name}";}
    private string ToPhysicalPath(string url)=>Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),url.TrimStart('/').Replace('/',Path.DirectorySeparatorChar));
    private static bool VerifyStripeSignature(string payload,string header,string secret){try{var parts=header.Split(',').Select(x=>x.Split('=',2)).Where(x=>x.Length==2).ToDictionary(x=>x[0],x=>x[1]);if(!parts.TryGetValue("t",out var t)||!parts.TryGetValue("v1",out var sig))return false;if(Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-long.Parse(t))>300)return false;using var h=new HMACSHA256(Encoding.UTF8.GetBytes(secret));var expected=Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes($"{t}.{payload}"))).ToLowerInvariant();return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected),Encoding.UTF8.GetBytes(sig));}catch{return false;}}
}
