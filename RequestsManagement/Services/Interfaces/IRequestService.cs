
using RequestsManagement.Models;
using RequestsManagement.DTOs;

namespace RequestsManagement.Services.Interfaces;

public interface IRequestService
{
    Task<Request?> GetByIdAsync(int id);

    Task<List<Request>> GetAllAsync();

    Task<Request> CreateAsync(Request request);

    Task<PagedResult<RequestDto>> GetPagedAsync(
        RequestFilterDto filter);

    Task<bool> UpdateStatusAsync(
        int id,
        UpdateRequestStatusDto dto);

    Task<int> BulkUpdateStatusAsync(
        BulkUpdateStatusDto dto,
        CancellationToken cancellationToken = default);

    Task<int> BulkUpdateMatchingAsync(
        BulkUpdateMatchingDto dto,
        CancellationToken cancellationToken = default);

    Task<List<RequestStatusHistoryDto>> GetStatusHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default);

    Task<RequestStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}
