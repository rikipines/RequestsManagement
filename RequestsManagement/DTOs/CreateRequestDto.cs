
using System.ComponentModel.DataAnnotations;
using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class CreateRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string OrganizationName { get; set; } = string.Empty;

    public RequestPriority Priority { get; set; } = RequestPriority.Low;

    public string? AssignedTo { get; set; }
}
