
using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class RequestDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }

    public RequestPriority Priority { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
