namespace Tattoo_Project.Models;

public class AccountDeletionAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DeletedAt { get; set; }
    public string RetentionNote { get; set; } = "Deletion audit retained for legal and security accountability; no user identifier is stored in this audit record.";
}
