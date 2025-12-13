using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaseDreams.Api.Data;

namespace TaseDreams.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfrastructureServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InfrastructureServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetInfrastructureServices()
    {
        var services = await _context.InfrastructureServices
            .Where(s => s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type
            })
            .ToListAsync();

        return Ok(services);
    }
}

