
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RequestsManagement.Data;
using System.Net;
using Xunit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RequestsManagement.Tests;

public class RequestsApiIntegrationTests
{
    [Fact]
    public async Task GetRequests_Returns200_FromRealApi()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Requests?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureTestServices(services =>
        {
            // Remove the original SQL Server DbContext registration
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Remove the original SQL Server provider configuration
            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            // Register SQLite instead of SQL Server
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
