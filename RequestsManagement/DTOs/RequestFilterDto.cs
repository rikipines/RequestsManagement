
using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class RequestFilterDto
{
    public string? Search { get; set; }

    public RequestStatus? Status { get; set; }

    public RequestPriority? Priority { get; set; }

    public string? AssignedTo { get; set; }

    public string? OrganizationName { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public string SortBy { get; set; } = "CreatedAt";

    public string SortDirection { get; set; } = "desc";

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
