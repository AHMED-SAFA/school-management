using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
public class ClassesController : ControllerBase
{
    private readonly AdmissionService _service;

    public ClassesController(AdmissionService service)
    {
        _service = service;
    }

    [HttpPost("/classes")]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        try
        {
            var result = await _service.CreateClassAsync(request);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    [HttpGet("/classes")]
    public async Task<IActionResult> GetClasses()
    {
        var result = await _service.GetAllClassesAsync();
        return Ok(result);
    }

    [HttpGet("/classes/{classId}")]
    public async Task<IActionResult> GetClass(string classId)
    {
        var result = await _service.GetClassAsync(classId);
        if (result == null)
            return NotFound(new ErrorResponse { Error = "Class not found." });
        return Ok(result);
    }
}
