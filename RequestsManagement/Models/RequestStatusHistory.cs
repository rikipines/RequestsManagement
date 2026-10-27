
namespace RequestsManagement.Models;

public class RequestStatusHistory
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public RequestStatus PreviousStatus { get; set; }

    public RequestStatus NewStatus { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string ChangedBy { get; set; } = string.Empty;

    public Request Request { get; set; } = null!;
}
