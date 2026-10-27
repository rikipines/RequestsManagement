
namespace RequestsManagement.Models;

public class Request
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.New;

    public RequestPriority Priority { get; set; } = RequestPriority.Low;

    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
