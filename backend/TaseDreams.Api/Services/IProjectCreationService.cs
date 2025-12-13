using TaseDreams.Api.Models.DTOs;

namespace TaseDreams.Api.Services;

public interface IProjectCreationService
{
    Task<ProjectCreationResponse> CreateProjectAsync(ProjectCreationRequest request);
}

