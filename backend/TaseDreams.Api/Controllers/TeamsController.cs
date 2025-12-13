using Microsoft.AspNetCore.Mvc;

namespace TaseDreams.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<string>> GetTeams()
    {
        var teams = new[]
        {
            "InnoTeam",
            "TP1Team",
            "BackOfficeTeam",
            "CtciTeam",
            "TradeTeam",
            "OrgAppsTeam",
            "DevOpsTeam"
        };

        return Ok(teams);
    }
}

