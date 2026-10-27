using RequestsManagement.Models;

namespace RequestsManagement.DTOs;

public class BulkUpdateMatchingDto
{
    public RequestFilterDto Filter { get; set; } = new();

    public RequestStatus NewStatus { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
}
