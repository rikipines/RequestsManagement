
using System.ComponentModel.DataAnnotations;
using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class BulkUpdateStatusDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<BulkStatusItemDto> Items { get; set; } = new();

    [Required]
    public string ChangedBy { get; set; } = string.Empty;
}

public class BulkStatusItemDto
{
    public int Id { get; set; }

    public RequestStatus NewStatus { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
