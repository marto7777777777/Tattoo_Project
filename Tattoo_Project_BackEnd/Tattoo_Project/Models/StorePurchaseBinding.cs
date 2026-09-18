namespace Tattoo_Project.Models;
public class StorePurchaseBinding
{
 public long Id { get; set; } public string Provider { get; set; }=null!;public string BindingKeyHash { get; set; }=null!;public int OriginalArtistSubscriptionId { get; set; }public DateTime CreatedAt { get; set; }
}
