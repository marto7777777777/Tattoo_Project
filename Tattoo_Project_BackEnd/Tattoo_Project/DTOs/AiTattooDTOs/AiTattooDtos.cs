using Microsoft.AspNetCore.Http;
namespace Tattoo_Project.DTOs.AiTattooDTOs;
public class CreateAiTattooProjectDto { public string Title {get;set;}=null!; public string TattooStyle {get;set;}=null!; public string Placement {get;set;}=null!; public string Description {get;set;}=null!; public IFormFile? ReferenceImage {get;set;} }
public class EditAiTattooProjectDto { public string Instruction {get;set;}=null!; public int? BaseVersionId {get;set;} }
public class AiTattooVersionDto { public int Id {get;set;} public int VersionNumber {get;set;} public string Prompt {get;set;}=null!; public string ImageUrl {get;set;}=null!; public DateTime CreatedAt {get;set;} }
public class AiTattooProjectDto { public int Id {get;set;} public string Title {get;set;}=null!; public string TattooStyle {get;set;}=null!; public string Placement {get;set;}=null!; public string InitialDescription {get;set;}=null!; public bool IsFreeProject {get;set;} public int FreeEditsUsed {get;set;} public int FreeEditsRemaining {get;set;} public DateTime? EditingAccessUntil {get;set;} public bool CanEdit {get;set;} public bool NeedsPayment {get;set;} public DateTime CreatedAt {get;set;} public List<AiTattooVersionDto> Versions {get;set;}=[]; }
public class CreateCheckoutDto { public int ProjectId {get;set;} }
public class CheckoutSessionDto { public string Url {get;set;}=null!; }
