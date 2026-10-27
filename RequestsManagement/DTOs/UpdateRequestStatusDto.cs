
using System.ComponentModel.DataAnnotations;
using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class UpdateRequestStatusDto
{
    [Required]
    public RequestStatus? NewStatus { get; set; }

    [Required]
    [StringLength(100)]
    public string ChangedBy { get; set; } = string.Empty;
}
