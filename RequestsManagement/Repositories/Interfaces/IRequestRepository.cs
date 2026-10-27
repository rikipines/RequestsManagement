
using Microsoft.EntityFrameworkCore;
using RequestsManagement.DTOs;
using RequestsManagement.Models;
namespace RequestsManagement.Repositories.Interfaces;

public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(int id);

    Task<List<Request>> GetAllAsync();

    Task AddAsync(Request request);

    Task SaveChangesAsync();
    Task<PagedResult<Request>> GetPagedAsync(RequestFilterDto filter);
    Task AddStatusHistoryAsync(RequestStatusHistory history);
    Task<List<Request>> GetByIdsAsync(
    IEnumerable<int> ids,
    CancellationToken cancellationToken = default);
    void SetOriginalRowVersion(
    Request request,
    byte[] rowVersion);
    Task<List<RequestStatusHistory>> GetStatusHistoryAsync(
    int requestId,
    CancellationToken cancellationToken = default);

    Task<RequestStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
    Task<List<Request>> GetMatchingBatchAsync(
    RequestFilterDto filter,
    int lastId,
    int batchSize,
    CancellationToken cancellationToken = default);
    Task<int> BulkUpdateMatchingAsync(
    BulkUpdateMatchingDto dto,
    CancellationToken cancellationToken = default);


    void ClearTracking();



}
