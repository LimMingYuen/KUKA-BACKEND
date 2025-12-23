using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.EmailRecipients;

/// <summary>
/// DTO for email recipient response
/// </summary>
public class EmailRecipientDto
{
    public int Id { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string NotificationTypes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request DTO for creating a new email recipient
/// </summary>
public class EmailRecipientCreateRequest
{
    [Required(ErrorMessage = "Email address is required")]
    [MaxLength(255)]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// Comma-separated notification types: MissionError, JobQueryError, SystemAlert
    /// </summary>
    [MaxLength(512)]
    public string NotificationTypes { get; set; } = "MissionError,JobQueryError";

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating an existing email recipient
/// </summary>
public class EmailRecipientUpdateRequest
{
    [Required(ErrorMessage = "Email address is required")]
    [MaxLength(255)]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    [MaxLength(512)]
    public string NotificationTypes { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for sending a test email
/// </summary>
public class TestEmailRequest
{
    [Required(ErrorMessage = "Test email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string TestEmailAddress { get; set; } = string.Empty;
}
