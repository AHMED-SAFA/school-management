using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
public class HoldsController : ControllerBase
{
    private readonly AdmissionService _service;

    public HoldsController(AdmissionService service)
    {
        _service = service;
    }

    [HttpPost("/classes/{classId}/holds")]
    public async Task<IActionResult> CreateHold(string classId)
    {
        var studentId = Request.Headers["X-Student-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new ErrorResponse { Error = "X-Student-Id header is required." });

        try
        {
            var result = await _service.CreateHoldAsync(classId, studentId);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Error = ex.Message });
        }
        catch (SeatUnavailableException ex)
        {
            return Conflict(new ErrorResponse { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    [HttpGet("/holds/{holdId}")]
    public async Task<IActionResult> GetHold(string holdId)
    {
        var result = await _service.GetHoldAsync(holdId);
        if (result == null)
            return NotFound(new ErrorResponse { Error = "Hold not found." });
        return Ok(result);
    }

    [HttpPost("/holds/{holdId}/confirm")]
    public async Task<IActionResult> ConfirmHold(string holdId)
    {
        var studentId = Request.Headers["X-Student-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new ErrorResponse { Error = "X-Student-Id header is required." });

        try
        {
            var result = await _service.ConfirmHoldAsync(holdId, studentId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    [HttpDelete("/holds/{holdId}")]
    public async Task<IActionResult> CancelHold(string holdId)
    {
        var studentId = Request.Headers["X-Student-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new ErrorResponse { Error = "X-Student-Id header is required." });

        try
        {
            await _service.CancelHoldAsync(holdId, studentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    [HttpGet("/me/holds")]
    public async Task<IActionResult> GetMyHolds()
    {
        var studentId = Request.Headers["X-Student-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new ErrorResponse { Error = "X-Student-Id header is required." });

        try
        {
            var result = await _service.GetMyHoldsAsync(studentId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }
}
