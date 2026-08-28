namespace Keepwise.Domain.Entities;

public sealed class User : SoftDeletableEntity
{
    public string FirebaseUid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string CountryCode { get; set; } = "IN";
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public string Language { get; set; } = "en";
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
}
