
using Microsoft.EntityFrameworkCore;
using RequestsManagement.DTOs;
using RequestsManagement.DTOs;
using RequestsManagement.DTOs;
using RequestsManagement.Models;
using RequestsManagement.Models;
using RequestsManagement.Models;
using RequestsManagement.Repositories.Interfaces;
using RequestsManagement.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace RequestsManagement.Services;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;
    private readonly IMemoryCache _cache;
    private const string StatisticsCacheKey = "requests_statistics";

    public RequestService(
      IRequestRepository repository,
      IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Request?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException(
                "Request ID must be greater than zero.");
        }

        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<Request>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Request> CreateAsync(Request request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException(
                "Request title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            throw new ArgumentException(
                "Organization name is required.");
        }

        request.Title = request.Title.Trim();
        request.OrganizationName = request.OrganizationName.Trim();

        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _repository.AddAsync(request);
        await _repository.SaveChangesAsync();
        _cache.Remove(StatisticsCacheKey);


        return request;
    }

    public async Task<PagedResult<RequestDto>> GetPagedAsync(
        RequestFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var result = await _repository.GetPagedAsync(filter);

        return new PagedResult<RequestDto>
        {
            Items = result.Items.Select(r => new RequestDto
            {
                Id = r.Id,
                Title = r.Title,
                OrganizationName = r.OrganizationName,
                Status = r.Status,
                Priority = r.Priority,
                AssignedTo = r.AssignedTo,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RowVersion = Convert.ToBase64String(r.RowVersion)
            }).ToList(),

            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        UpdateRequestStatusDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.NewStatus is null ||
            !Enum.IsDefined(dto.NewStatus.Value))
        {
            throw new ArgumentException("Invalid status.");
        }

        if (string.IsNullOrWhiteSpace(dto.ChangedBy))
        {
            throw new ArgumentException("ChangedBy is required.");
        }

        var request = await _repository.GetByIdAsync(id);

        if (request == null)
            return false;

        var newStatus = dto.NewStatus.Value;

        if (request.Status == newStatus)
            return true;

        var history = new RequestStatusHistory
        {
            RequestId = request.Id,
            PreviousStatus = request.Status,
            NewStatus = newStatus,
            ChangedBy = dto.ChangedBy.Trim(),
            ChangedAt = DateTime.UtcNow
        };

        request.Status = newStatus;
        request.UpdatedAt = DateTime.UtcNow;

        await _repository.AddStatusHistoryAsync(history);
        _cache.Remove(StatisticsCacheKey);

        // שומר את שינוי הסטטוס וההיסטוריה יחד
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<int> BulkUpdateStatusAsync(
        BulkUpdateStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Items == null ||
            dto.Items.Count == 0 ||
            dto.Items.Count > 100)
        {
            throw new ArgumentException(
                "Bulk update must contain between 1 and 100 requests.");
        }

        if (string.IsNullOrWhiteSpace(dto.ChangedBy))
        {
            throw new ArgumentException(
                "ChangedBy is required.");
        }

        var ids = dto.Items.Select(item => item.Id).ToList();

        if (ids.Any(id => id <= 0) ||
            ids.Distinct().Count() != ids.Count)
        {
            throw new ArgumentException(
                "Request IDs must be positive and unique.");
        }

        foreach (var item in dto.Items)
        {
            if (!Enum.IsDefined(item.NewStatus))
            {
                throw new ArgumentException(
                    $"Invalid status for request {item.Id}.");
            }

            if (string.IsNullOrWhiteSpace(item.RowVersion))
            {
                throw new ArgumentException(
                    $"RowVersion is required for request {item.Id}.");
            }
        }

        var requests = await _repository.GetByIdsAsync(
            ids, cancellationToken);

        if (requests.Count != ids.Count)
        {
            throw new KeyNotFoundException(
                "One or more requests were not found.");
        }

        var requestsById = requests.ToDictionary(r => r.Id);

        foreach (var item in dto.Items)
        {
            var request = requestsById[item.Id];

            byte[] expectedVersion;

            try
            {
                expectedVersion = Convert.FromBase64String(
                    item.RowVersion);
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    $"Invalid RowVersion for request {item.Id}.");
            }


            if (!request.RowVersion.SequenceEqual(expectedVersion))
            {
                throw new DbUpdateConcurrencyException(
                    $"Concurrency conflict for request {item.Id}.");
            }

            _repository.SetOriginalRowVersion(
                request,
                expectedVersion);

        }

        var changedCount = 0;
        var changedAt = DateTime.UtcNow;

        foreach (var item in dto.Items)
        {
            var request = requestsById[item.Id];

            if (request.Status == item.NewStatus)
                continue;

            var history = new RequestStatusHistory
            {
                RequestId = request.Id,
                PreviousStatus = request.Status,
                NewStatus = item.NewStatus,
                ChangedBy = dto.ChangedBy.Trim(),
                ChangedAt = changedAt
            };

            request.Status = item.NewStatus;
            request.UpdatedAt = changedAt;

            await _repository.AddStatusHistoryAsync(history);

            changedCount++;
        }

        if (changedCount > 0)
        {
            await _repository.SaveChangesAsync();
        }

        _cache.Remove(StatisticsCacheKey);

        return changedCount;
    }

    public async Task<List<RequestStatusHistoryDto>> GetStatusHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        if (requestId <= 0)
            throw new ArgumentException("Invalid request ID.");

        var request = await _repository.GetByIdAsync(requestId);

        if (request == null)
            throw new KeyNotFoundException("Request not found.");

        var history = await _repository.GetStatusHistoryAsync(
            requestId, cancellationToken);

        return history.Select(h => new RequestStatusHistoryDto
        {
            Id = h.Id,
            RequestId = h.RequestId,
            PreviousStatus = h.PreviousStatus,
            NewStatus = h.NewStatus,
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy
        }).ToList();
    }


    public async Task<RequestStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var statistics = await _cache.GetOrCreateAsync(
            StatisticsCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(60);

                return await _repository.GetStatisticsAsync(
                    cancellationToken);
            });

        return statistics!;
    }


    public async Task<int> BulkUpdateMatchingAsync(
     BulkUpdateMatchingDto dto,
     CancellationToken cancellationToken = default)
    {
        var updatedCount =
            await _repository.BulkUpdateMatchingAsync(
                dto, cancellationToken);

        _cache.Remove(StatisticsCacheKey);

        return updatedCount;
    }


}
