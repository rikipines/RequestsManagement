
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.DTOs;
using RequestsManagement.Models;
using RequestsManagement.Repositories.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace RequestsManagement.Repositories;

public class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _context;

    public RequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Request?> GetByIdAsync(int id)
    {
        return await _context.Requests
            .FirstOrDefaultAsync(r => r.Id == id);
    }



    public async Task<PagedResult<Request>> GetPagedAsync(
        RequestFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.PageNumber < 1 ||
            filter.PageSize < 1 ||
            filter.PageSize > 100)
        {
            throw new ArgumentException(
                "PageNumber must be positive and PageSize must be between 1 and 100.");
        }

        if (filter.CreatedFrom.HasValue &&
            filter.CreatedTo.HasValue &&
            filter.CreatedFrom > filter.CreatedTo)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            throw new ArgumentException("Invalid status.");
        }

        if (filter.Priority.HasValue &&
            !Enum.IsDefined(filter.Priority.Value))
        {
            throw new ArgumentException("Invalid priority.");
        }

        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        var sortDirection = filter.SortDirection?.Trim().ToLowerInvariant();

        if (sortBy is not (
     "createdat" or
     "updatedat" or
     "priority" or
     "title" or
     "id" or
     "organizationname" or
     "status" or
     "assignedto"))
        {
            throw new ArgumentException("Invalid SortBy.");
        }

        IQueryable<Request> query = _context.Requests
            .AsNoTracking();

        // Search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(r =>
                r.Title.Contains(search) ||
                r.OrganizationName.Contains(search));
        }

        // Filters
        if (filter.Status.HasValue)
        {
            query = query.Where(r =>
                r.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(r =>
                r.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            var assignedTo = filter.AssignedTo.Trim();

            query = query.Where(r =>
                r.AssignedTo != null &&
                r.AssignedTo.Contains(assignedTo));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrganizationName))
        {
            var organizationName = filter.OrganizationName.Trim();

            query = query.Where(r =>
                r.OrganizationName.Contains(organizationName));
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(r =>
                r.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(r =>
                r.CreatedAt <= filter.CreatedTo.Value);
        }
        Console.WriteLine("SQL QUERY:");
        Console.WriteLine(query.ToQueryString());
        // Count matching records before pagination
        var totalCount = await query.CountAsync();

        // Stable sorting for consistent pagination
        // Stable sorting for consistent pagination
        IOrderedQueryable<Request> orderedQuery =
            (sortBy, sortDirection) switch
            {
                // CreatedAt
                ("createdat", "asc") =>
                    query.OrderBy(r => r.CreatedAt)
                         .ThenBy(r => r.Id),

                ("createdat", "desc") =>
                    query.OrderByDescending(r => r.CreatedAt)
                         .ThenByDescending(r => r.Id),

                // UpdatedAt
                ("updatedat", "asc") =>
                    query.OrderBy(r => r.UpdatedAt)
                         .ThenBy(r => r.Id),

                ("updatedat", "desc") =>
                    query.OrderByDescending(r => r.UpdatedAt)
                         .ThenByDescending(r => r.Id),

                // Priority
                ("priority", "asc") =>
                    query.OrderBy(r => r.Priority)
                         .ThenBy(r => r.Id),

                ("priority", "desc") =>
                    query.OrderByDescending(r => r.Priority)
                         .ThenByDescending(r => r.Id),

                // Title
                ("title", "asc") =>
                    query.OrderBy(r => r.Title)
                         .ThenBy(r => r.Id),

                ("title", "desc") =>
                    query.OrderByDescending(r => r.Title)
                         .ThenByDescending(r => r.Id),

                // Id
                ("id", "asc") =>
                    query.OrderBy(r => r.Id),

                ("id", "desc") =>
                    query.OrderByDescending(r => r.Id),

                // OrganizationName
                ("organizationname", "asc") =>
                    query.OrderBy(r => r.OrganizationName)
                         .ThenBy(r => r.Id),

                ("organizationname", "desc") =>
                    query.OrderByDescending(r => r.OrganizationName)
                         .ThenByDescending(r => r.Id),

                // Status
                ("status", "asc") =>
                    query.OrderBy(r => r.Status)
                         .ThenBy(r => r.Id),

                ("status", "desc") =>
                    query.OrderByDescending(r => r.Status)
                         .ThenByDescending(r => r.Id),

                // AssignedTo
                ("assignedto", "asc") =>
                    query.OrderBy(r => r.AssignedTo)
                         .ThenBy(r => r.Id),

                ("assignedto", "desc") =>
                    query.OrderByDescending(r => r.AssignedTo)
                         .ThenByDescending(r => r.Id),

                // Default
                _ =>
                    query.OrderByDescending(r => r.CreatedAt)
                         .ThenByDescending(r => r.Id)
            };

        //var items = await orderedQuery
        //    .Skip((filter.PageNumber - 1) * filter.PageSize)
        //    .Take(filter.PageSize)
        //    .ToListAsync();

        var pagedQuery = orderedQuery
    .Skip((filter.PageNumber - 1) * filter.PageSize)
    .Take(filter.PageSize);

        var sql = pagedQuery.ToQueryString();

        System.Diagnostics.Debug.WriteLine(
            "===== EF CORE PAGING SQL =====");

        System.Diagnostics.Debug.WriteLine(sql);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var items = await pagedQuery.ToListAsync();

        stopwatch.Stop();

        System.Diagnostics.Debug.WriteLine(
            $"===== PAGING TIME: {stopwatch.ElapsedMilliseconds} ms =====");

        return new PagedResult<Request>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }


    public async Task AddAsync(Request request)
    {
        await _context.Requests.AddAsync(request);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    //public async Task<PagedResult<Request>> GetPagedAsync(
    //    RequestFilterDto filter)
    //{
    //    var query = _context.Requests
    //        .AsNoTracking()
    //        .AsQueryable();

    //    if (!string.IsNullOrWhiteSpace(filter.Search))
    //    {
    //        var search = filter.Search.Trim();

    //        query = query.Where(r =>
    //            r.Title.Contains(search) ||
    //            r.OrganizationName.Contains(search));
    //    }

    //    if (filter.Status.HasValue)
    //    {
    //        query = query.Where(r =>
    //            r.Status == filter.Status.Value);
    //    }

    //    if (filter.Priority.HasValue)
    //    {
    //        query = query.Where(r =>
    //            r.Priority == filter.Priority.Value);
    //    }

    //    if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
    //    {
    //        var assignedTo = filter.AssignedTo.Trim();

    //        query = query.Where(r =>
    //            r.AssignedTo == assignedTo);
    //    }

    //    var totalCount = await query.CountAsync();

    //    var pageNumber = Math.Max(1, filter.PageNumber);
    //    var pageSize = Math.Clamp(filter.PageSize, 1, 100);

    //    var items = await query
    //        .OrderByDescending(r => r.CreatedAt)
    //        .ThenByDescending(r => r.Id)
    //        .Skip((pageNumber - 1) * pageSize)
    //        .Take(pageSize)
    //        .ToListAsync();

    //    return new PagedResult<Request>
    //    {
    //        Items = items,
    //        TotalCount = totalCount,
    //        PageNumber = pageNumber,
    //        PageSize = pageSize
    //    };
    //}

    public async Task AddStatusHistoryAsync(
        RequestStatusHistory history)
    {
        await _context.RequestStatusHistories.AddAsync(history);
    }

    public async Task<List<Request>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();

        return await _context.Requests
            .Where(r => idList.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Request>> GetAllAsync()
    {
        return await _context.Requests
            .AsNoTracking()
            .ToListAsync();
    }

    public void SetOriginalRowVersion(
    Request request,
    byte[] rowVersion)
    {
        _context.Entry(request)
            .Property(r => r.RowVersion)
            .OriginalValue = rowVersion;
    }

    public async Task<List<RequestStatusHistory>> GetStatusHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RequestStatusHistories
            .AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<RequestStatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalRequests = await _context.Requests
            .CountAsync(cancellationToken);

        var statusCounts = await _context.Requests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);
	

        var priorityCounts = await _context.Requests
            .AsNoTracking()
            .GroupBy(r => r.Priority)
            .Select(g => new
            {
                Priority = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return new RequestStatisticsDto
        {
            TotalRequests = totalRequests,

            ByStatus = statusCounts.Select(x => new StatusCountDto
            {
                Status = x.Status.ToString(),
                Count = x.Count
            }).ToList(),

            ByPriority = priorityCounts.Select(x => new PriorityCountDto
            {
                Priority = x.Priority.ToString(),
                Count = x.Count
            }).ToList()
        };
    }
    public async Task<List<Request>> GetMatchingBatchAsync(
    RequestFilterDto filter,
    int lastId,
    int batchSize,
    CancellationToken cancellationToken = default)
    {
        if (batchSize < 1 || batchSize > 500)
            throw new ArgumentException("Invalid batch size.");

        IQueryable<Request> query = _context.Requests
            .Where(r => r.Id > lastId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(r =>
                r.Title.Contains(search) ||
                r.OrganizationName.Contains(search));
        }

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(r => r.Priority == filter.Priority.Value);

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            var assignedTo = filter.AssignedTo.Trim();

            query = query.Where(r =>
                r.AssignedTo != null &&
                r.AssignedTo.Contains(assignedTo));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrganizationName))
        {
            var organizationName = filter.OrganizationName.Trim();

            query = query.Where(r =>
                r.OrganizationName.Contains(organizationName));
        }

        if (filter.CreatedFrom.HasValue)
            query = query.Where(r =>
                r.CreatedAt >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(r =>
                r.CreatedAt <= filter.CreatedTo.Value);

        return await query
            .OrderBy(r => r.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }

    public async Task<int> BulkUpdateMatchingAsync(
        BulkUpdateMatchingDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(dto.Filter);

        if (!Enum.IsDefined(dto.NewStatus))
            throw new ArgumentException("Invalid new status.");

        var filter = dto.Filter;

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
            throw new ArgumentException("Invalid filter status.");

        if (filter.Priority.HasValue &&
            !Enum.IsDefined(filter.Priority.Value))
            throw new ArgumentException("Invalid filter priority.");

        if (filter.CreatedFrom.HasValue &&
            filter.CreatedTo.HasValue &&
            filter.CreatedFrom > filter.CreatedTo)
            throw new ArgumentException("Invalid date range.");

        var changedBy = dto.ChangedBy?.Trim();

        if (string.IsNullOrWhiteSpace(changedBy) ||
            changedBy.Length > 100)
            throw new ArgumentException("Invalid ChangedBy.");

        const string sql = """
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ChangedAt datetime2(7) = SYSUTCDATETIME();
    DECLARE @UpdatedCount int = 0;

    DECLARE @Changed TABLE
    (
        RequestId int NOT NULL,
        PreviousStatus int NOT NULL,
        NewStatus int NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE r
        SET
            r.Status = @NewStatus,
            r.UpdatedAt = @ChangedAt
        OUTPUT
            inserted.Id,
            deleted.Status,
            inserted.Status
        INTO @Changed
        (
            RequestId,
            PreviousStatus,
            NewStatus
        )
        FROM dbo.Requests AS r
        WHERE r.Status <> @NewStatus
          AND (
              @Search IS NULL
              OR CHARINDEX(@Search, r.Title) > 0
              OR CHARINDEX(@Search, r.OrganizationName) > 0
          )
          AND (@Status IS NULL OR r.Status = @Status)
          AND (@Priority IS NULL OR r.Priority = @Priority)
          AND (
              @AssignedTo IS NULL
              OR CHARINDEX(@AssignedTo, r.AssignedTo) > 0
          )
          AND (
              @OrganizationName IS NULL
              OR CHARINDEX(@OrganizationName, r.OrganizationName) > 0
          )
          AND (@CreatedFrom IS NULL OR r.CreatedAt >= @CreatedFrom)
          AND (@CreatedTo IS NULL OR r.CreatedAt <= @CreatedTo);

        SET @UpdatedCount = @@ROWCOUNT;

        INSERT INTO dbo.RequestStatusHistories
        (
            RequestId,
            PreviousStatus,
            NewStatus,
            ChangedAt,
            ChangedBy
        )
        SELECT
            RequestId,
            PreviousStatus,
            NewStatus,
            @ChangedAt,
            @ChangedBy
        FROM @Changed;

        COMMIT TRANSACTION;

        SELECT @UpdatedCount;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
    """;

        static object DbValue(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : value.Trim();

        var parameters = new[]
        {
        new SqlParameter("@NewStatus", SqlDbType.Int)
        {
            Value = (int)dto.NewStatus
        },
        new SqlParameter("@ChangedBy", SqlDbType.NVarChar, 100)
        {
            Value = changedBy
        },
        new SqlParameter("@Search", SqlDbType.NVarChar, 200)
        {
            Value = DbValue(filter.Search)
        },
        new SqlParameter("@Status", SqlDbType.Int)
        {
            Value = filter.Status.HasValue
                ? (object)(int)filter.Status.Value
                : DBNull.Value
        },
        new SqlParameter("@Priority", SqlDbType.Int)
        {
            Value = filter.Priority.HasValue
                ? (object)(int)filter.Priority.Value
                : DBNull.Value
        },
        new SqlParameter("@AssignedTo", SqlDbType.NVarChar, 200)
        {
            Value = DbValue(filter.AssignedTo)
        },
        new SqlParameter("@OrganizationName", SqlDbType.NVarChar, 200)
        {
            Value = DbValue(filter.OrganizationName)
        },
        new SqlParameter("@CreatedFrom", SqlDbType.DateTime2)
        {
            Value = filter.CreatedFrom.HasValue
                ? (object)filter.CreatedFrom.Value
                : DBNull.Value
        },
        new SqlParameter("@CreatedTo", SqlDbType.DateTime2)
        {
            Value = filter.CreatedTo.HasValue
                ? (object)filter.CreatedTo.Value
                : DBNull.Value
        }
    };

        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 120;

            foreach (var parameter in parameters)
                command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            _context.ChangeTracker.Clear();

            return Convert.ToInt32(result);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }



}
