
namespace RequestsManagement.DTOs;

public class RequestStatisticsDto
{
    public int TotalRequests { get; set; }

    public List<StatusCountDto> ByStatus { get; set; } = new();

    public List<PriorityCountDto> ByPriority { get; set; } = new();
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class PriorityCountDto
{
    public string Priority { get; set; } = string.Empty;

    public int Count { get; set; }
}
