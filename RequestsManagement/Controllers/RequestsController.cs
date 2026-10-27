
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.DTOs;
using RequestsManagement.Models;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Services.Interfaces;

namespace RequestsManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;

    public RequestsController(IRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestDto>>> GetPaged(
        [FromQuery] RequestFilterDto filter)
    {
        var result = await _requestService.GetPagedAsync(filter);

        return Ok(result);
    }


    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestDto>> GetById(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid request ID.");

        var request = await _requestService.GetByIdAsync(id);

        if (request == null)
            return NotFound();

        return Ok(MapToDto(request));
    }

    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(
        [FromBody] CreateRequestDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var request = new Request
        {
            Title = dto.Title,
            OrganizationName = dto.OrganizationName,
            Priority = dto.Priority,
            AssignedTo = dto.AssignedTo,
            Status = RequestStatus.New
        };

        var created = await _requestService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            MapToDto(created));
    }

    private static RequestDto MapToDto(Request request)
    {
        return new RequestDto
        {
            Id = request.Id,
            Title = request.Title,
            OrganizationName = request.OrganizationName,
            Status = request.Status,
            Priority = request.Priority,
            AssignedTo = request.AssignedTo,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateRequestStatusDto dto)
    {
        if (id <= 0)
            return BadRequest("Invalid request ID.");

        if (dto.NewStatus is null ||
            !Enum.IsDefined(dto.NewStatus.Value))
        {
            return BadRequest("Invalid status.");
        }

        if (string.IsNullOrWhiteSpace(dto.ChangedBy))
            return BadRequest("ChangedBy is required.");

        var updated = await _requestService.UpdateStatusAsync(id, dto);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("bulk/status")]
    public async Task<IActionResult> BulkUpdateStatus(
        [FromBody] BulkUpdateStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedCount =
                await _requestService.BulkUpdateStatusAsync(
                    dto, cancellationToken);

            return Ok(new
            {
                Message = "Bulk update completed successfully.",
                UpdatedCount = updatedCount
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
   

        catch (DbUpdateConcurrencyException ex)
        {
            return Conflict(new
            {
                Message = "The request was updated by another user. Please refresh and try again."
            });
        }

    }


    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetStatusHistory(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest(new { Message = "Invalid request ID." });

        try
        {
            var history = await _requestService.GetStatusHistoryAsync(
                id, cancellationToken);

            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "Request not found." });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        CancellationToken cancellationToken)
    {
        var statistics = await _requestService.GetStatisticsAsync(
            cancellationToken);

        return Ok(statistics);
    }
    [HttpPatch("bulk/matching/status")]
    public async Task<IActionResult> BulkUpdateMatchingStatus(
    [FromBody] BulkUpdateMatchingDto dto,
    CancellationToken cancellationToken)
    {
        try
        {
            var updatedCount =
                await _requestService.BulkUpdateMatchingAsync(
                    dto,
                    cancellationToken);

            return Ok(new
            {
                Message = "Matching requests updated successfully.",
                UpdatedCount = updatedCount
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                Message = "Concurrency conflict during bulk update."
            });
        }
    }



}
