using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaseDreams.Api.Data;
using TaseDreams.Api.Models;
using TaseDreams.Api.Models.DTOs;
using TaseDreams.Api.Services;

namespace TaseDreams.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectCreationService _projectCreationService;
    private readonly ApplicationDbContext _context;

    public ProjectsController(IProjectCreationService projectCreationService, ApplicationDbContext context)
    {
        _projectCreationService = projectCreationService;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectCreationResponse>> CreateProject([FromBody] ProjectCreationRequest request)
    {
        try
        {
            var response = await _projectCreationService.CreateProjectAsync(request);
            
            if (response.Status == ProjectStatus.Failed)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Catch any unhandled exceptions and return detailed error
            var rootCause = ex.InnerException?.Message ?? ex.Message;
            var detailedError = ex.InnerException?.ToString() ?? ex.ToString();
            
            return BadRequest(new ProjectCreationResponse
            {
                ProjectName = request.ProjectName,
                Status = ProjectStatus.Failed,
                Message = $"Unexpected error: {rootCause}",
                ErrorDetails = detailedError
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetProjects()
    {
        var projects = await _context.Projects
            .Include(p => p.ProjectLane)
            .Include(p => p.InfrastructureServices)
            .ThenInclude(pi => pi.InfrastructureService)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.BitbucketProjectKey,
                p.DevelopmentTeam,
                ProjectLane = p.ProjectLane.Name,
                p.BackendVersion,
                p.FrontendVersion,
                p.Status,
                p.CreatedAt,
                p.CompletedAt,
                InfrastructureServices = p.InfrastructureServices.Select(pi => pi.InfrastructureService.Name)
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectLane)
            .Include(p => p.InfrastructureServices)
            .ThenInclude(pi => pi.InfrastructureService)
            .Include(p => p.NodePorts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            project.Id,
            project.Name,
            project.BitbucketProjectKey,
            project.DevelopmentTeam,
            ProjectLane = project.ProjectLane.Name,
            project.BackendVersion,
            project.FrontendVersion,
            project.Status,
            project.CreatedAt,
            project.CompletedAt,
            project.ErrorMessage,
            InfrastructureServices = project.InfrastructureServices.Select(pi => pi.InfrastructureService.Name),
            NodePorts = project.NodePorts.Select(np => new { np.ServiceName, np.Port })
        });
    }
}

