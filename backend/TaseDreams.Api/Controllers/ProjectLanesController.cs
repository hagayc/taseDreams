using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaseDreams.Api.Data;

namespace TaseDreams.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectLanesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectLanesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetProjectLanes()
    {
        var lanes = await _context.ProjectLanes
            .Where(l => l.IsActive)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.BackendLanguage,
                l.FrontendFramework
            })
            .ToListAsync();

        return Ok(lanes);
    }
}

