using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

[ApiController]
[Route("api/environments")]
[Tags("Environments")]
[Authorize] // reference data: any authenticated user
public class EnvironmentsController : ControllerBase
{
    private readonly GmsDbContext _db;

    public EnvironmentsController(GmsDbContext db)
    {
        _db = db;
    }

    /// <summary>Ortamları döndürür. İsteğe bağlı olarak projeye göre filtreler.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnvironmentDto>>> GetAll([FromQuery] Guid? projectId)
    {
        var query = _db.Environments.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == projectId.Value);
        }

        var items = await query
            .OrderBy(e => e.Project!.Name)
            .ThenBy(e => e.Name)
            .Select(e => new EnvironmentDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                ProjectName = e.Project!.Name,
                Name = e.Name,
                Type = e.Type,
                Status = e.Status
            })
            .ToListAsync();

        return Ok(items);
    }
}
