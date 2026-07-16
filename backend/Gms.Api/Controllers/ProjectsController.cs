using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Tags("Projects")]
[Authorize] // reference data: any authenticated user
public class ProjectsController : ControllerBase
{
    private readonly GmsDbContext _db;

    public ProjectsController(GmsDbContext db)
    {
        _db = db;
    }

    /// <summary>Projeleri döndürür. İsteğe bağlı olarak müşteriye göre filtreler.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll([FromQuery] Guid? customerId)
    {
        var query = _db.Projects.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(p => p.CustomerId == customerId.Value);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer!.Name,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}
