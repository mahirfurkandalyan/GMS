using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Tags("Customers")]
[Authorize] // reference data: any authenticated user
public class CustomersController : ControllerBase
{
    private readonly GmsDbContext _db;

    public CustomersController(GmsDbContext db)
    {
        _db = db;
    }

    /// <summary>Tüm müşterileri döndürür.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var items = await _db.Customers
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}
