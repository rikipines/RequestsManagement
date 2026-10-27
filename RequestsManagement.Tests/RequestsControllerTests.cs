
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RequestsManagement.Controllers;
using RequestsManagement.DTOs;
using RequestsManagement.Models;
using RequestsManagement.Services.Interfaces;
using System.Timers;
using Xunit;

namespace RequestsManagement.Tests;

public class RequestsControllerTests
{
    [Fact]
    public async Task GetPaged_ReturnsFilteredResults()
    {
        var service = new Mock<IRequestService>();

        var filter = new RequestFilterDto
        {
            Status = RequestStatus.New,
            PageNumber = 1,
            PageSize = 10
        };

        var expected = new PagedResult<RequestDto>
        {
            Items = new List<RequestDto>
            {
                new RequestDto
                {
                    Id = 1,
                    Title = "Test Request",
                    Status = RequestStatus.New
                }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        service.Setup(s => s.GetPagedAsync(filter))
            .ReturnsAsync(expected);

        var controller = new RequestsController(service.Object);

        var response = await controller.GetPaged(filter);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<PagedResult<RequestDto>>(ok.Value);

        Assert.Single(result.Items);
        Assert.Equal(RequestStatus.New, result.Items[0].Status);

        service.Verify(s => s.GetPagedAsync(filter), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsBadRequest()
    {
        var service = new Mock<IRequestService>();
        var controller = new RequestsController(service.Object);

        var dto = new UpdateRequestStatusDto
        {
            NewStatus = (RequestStatus)999,
            ChangedBy = "Tester"
        };

        var response = await controller.UpdateStatus(1, dto);

        Assert.IsType<BadRequestObjectResult>(response);

        service.Verify(
            s => s.UpdateStatusAsync(
                It.IsAny<int>(),
                It.IsAny<UpdateRequestStatusDto>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkUpdate_ConcurrencyConflict_Returns409()
    {
        var service = new Mock<IRequestService>();

        service.Setup(s => s.BulkUpdateStatusAsync(
                It.IsAny<BulkUpdateStatusDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException(
                "Concurrency conflict"));

        var controller = new RequestsController(service.Object);

        var response = await controller.BulkUpdateStatus(
            new BulkUpdateStatusDto(),
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(response);

        Assert.Equal(409, conflict.StatusCode);
    }
}
