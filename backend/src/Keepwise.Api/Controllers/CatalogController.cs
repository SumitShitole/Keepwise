using Keepwise.Application.Catalog;
using Keepwise.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keepwise.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/catalog")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken cancellationToken) =>
        Ok(await catalog.ListAsync(cancellationToken));
}

[ApiController]
[Authorize]
[Route("v1/dashboard")]
public sealed class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken) =>
        Ok(await dashboard.GetAsync(cancellationToken));
}
