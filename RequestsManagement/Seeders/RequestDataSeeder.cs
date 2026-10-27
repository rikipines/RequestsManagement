
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.Models;

namespace RequestsManagement.Seeders;

public static class RequestDataSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        int targetCount = 100_000)
    {
        var existingCount = await context.Requests.CountAsync();

        if (existingCount >= targetCount)
            return;

        const int batchSize = 1000;

        string[] organizations =
        {
            "Jerusalem Municipality",
            "Tel Aviv Municipality",
            "Haifa Municipality",
            "Ministry of Welfare",
            "Ministry of Education"
        };

        string[] employees =
        {
            "Dana",
            "Sarah",
            "David",
            "Rachel",
            "Moshe"
        };

        var random = new Random(42);

        for (int offset = existingCount;
             offset < targetCount;
             offset += batchSize)
        {
            var count = Math.Min(
                batchSize,
                targetCount - offset);

            var requests = new List<Request>(count);

            for (int i = 0; i < count; i++)
            {
                var number = offset + i + 1;

                var createdAt = DateTime.UtcNow
                    .AddDays(-random.Next(0, 365));

                requests.Add(new Request
                {
                    Title = $"Test Request {number}",
                    OrganizationName =
                        organizations[random.Next(organizations.Length)],
                    Status = (RequestStatus)random.Next(0, 4),
                    Priority = (RequestPriority)random.Next(0, 3),
                    AssignedTo =
                        employees[random.Next(employees.Length)],
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            await context.Requests.AddRangeAsync(requests);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }
}
