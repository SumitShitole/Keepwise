using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Application.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Catalog;

public sealed class CatalogService(IKeepwiseDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Include(c => c.ItemTypes)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ItemTypes.OrderBy(t => t.Name).Select(t => new ItemTypeDto(t.Id, t.Name, t.Slug)).ToList()))
            .ToList();
    }
}
