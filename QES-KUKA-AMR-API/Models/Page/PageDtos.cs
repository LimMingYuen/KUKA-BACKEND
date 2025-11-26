using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.Page;

public class PageDto
{
    public int Id { get; set; }
    public string PagePath { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? PageIcon { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class PageCreateRequest
{
    [Required]
    [MaxLength(255)]
    public string PagePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PageName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PageIcon { get; set; }
}

public class PageUpdateRequest
{
    [Required]
    [MaxLength(255)]
    public string PagePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PageName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PageIcon { get; set; }
}

public class PageSyncRequest
{
    [Required]
    public List<PageCreateRequest> Pages { get; set; } = new();
}

public class PageSyncResponse
{
    public int TotalPages { get; set; }
    public int NewPages { get; set; }
    public int UpdatedPages { get; set; }
    public int UnchangedPages { get; set; }
}
