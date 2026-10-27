
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.Models;
using Xunit;

namespace RequestsManagement.Tests;

public class ConcurrencyIntegrationTests
{
    private const string ConnectionString =
        "Server=localhost;" +
        "Database=RequestsManagementConcurrencyTestsDb;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True;";

    [Fact]
    public async Task UpdatingWithStaleRowVersion_ThrowsConcurrencyException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        // Prepare an isolated test database.
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        int requestId;

        // Create a request for this test.
        await using (var setup = new AppDbContext(options))
        {
            var request = new Request
            {
                Title = "Concurrency Test",
                OrganizationName = "Test Organization",
                Status = RequestStatus.New,
                Priority = RequestPriority.Medium,
                AssignedTo = "Tester",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            setup.Requests.Add(request);
            await setup.SaveChangesAsync();

            requestId = request.Id;
        }

        // Two users load the same request.
        await using var user1 = new AppDbContext(options);
        await using var user2 = new AppDbContext(options);

        var request1 = await user1.Requests
            .SingleAsync(r => r.Id == requestId);

        var request2 = await user2.Requests
            .SingleAsync(r => r.Id == requestId);

        var originalVersion = request2.RowVersion.ToArray();

        // User 1 updates the request first.
        request1.Status = RequestStatus.InProgress;
        request1.UpdatedAt = DateTime.UtcNow;

        await user1.SaveChangesAsync();

        // User 2 still holds the old RowVersion.
        Assert.Equal(originalVersion, request2.RowVersion);

        request2.Status = RequestStatus.Completed;
        request2.UpdatedAt = DateTime.UtcNow;

        // SQL Server must reject the stale update.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await user2.SaveChangesAsync());

        // Verify that user 1's update was preserved.
        await using var verify = new AppDbContext(options);

        var savedStatus = await verify.Requests
            .Where(r => r.Id == requestId)
            .Select(r => r.Status)
            .SingleAsync();

        Assert.Equal(RequestStatus.InProgress, savedStatus);
    }
}
