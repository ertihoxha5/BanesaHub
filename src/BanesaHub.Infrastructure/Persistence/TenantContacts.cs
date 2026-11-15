using System.Linq.Expressions;
using BanesaHub.Application.Tenants;
using BanesaHub.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class TenantContacts(BanesaHubDbContext database) : ITenantContacts
{
    private static readonly Expression<Func<Tenant, TenantDetails>> ToDetails =
        tenant => new TenantDetails(tenant.Id, tenant.FullName, tenant.Email, tenant.Phone);

    public async Task<IReadOnlyList<TenantDetails>?> ListAsync(Guid organizationId, Guid actorId, int offset,
        int limit, string? search, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        var tenants = Visible(organizationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            tenants = tenants.Where(tenant => tenant.FullName.Contains(term));
        }

        return await tenants.OrderBy(tenant => tenant.FullName).ThenBy(tenant => tenant.Id)
            .Skip(offset).Take(limit).Select(ToDetails).ToListAsync(ct);
    }

    public async Task<TenantDetails?> FindAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId).Where(tenant => tenant.Id == id)
            .Select(ToDetails).SingleOrDefaultAsync(ct);
    }

    public async Task<TenantDetails?> CreateAsync(Guid organizationId, Guid actorId, TenantInput input, CancellationToken ct)
    {
        if (!await PropertyInventory.CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FullName = input.FullName,
            Email = input.Email,
            Phone = input.Phone
        };
        database.Tenants.Add(tenant);
        await database.SaveChangesAsync(ct);
        return new TenantDetails(tenant.Id, tenant.FullName, tenant.Email, tenant.Phone);
    }

    public async Task<bool> UpdateAsync(Guid organizationId, Guid actorId, Guid id, TenantInput input, CancellationToken ct)
    {
        var tenant = await FindEditable(organizationId, actorId, id, ct);
        if (tenant is null)
        {
            return false;
        }

        tenant.FullName = input.FullName;
        tenant.Email = input.Email;
        tenant.Phone = input.Phone;
        await database.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct)
    {
        var tenant = await FindEditable(organizationId, actorId, id, ct);
        if (tenant is null)
        {
            return false;
        }

        tenant.IsArchived = true;
        await database.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Tenant> Visible(Guid organizationId) =>
        database.Tenants.Where(tenant => tenant.OrganizationId == organizationId && !tenant.IsArchived);

    private async Task<Tenant?> FindEditable(Guid organizationId, Guid actorId, Guid id, CancellationToken ct) =>
        await PropertyInventory.CanManage(database, organizationId, actorId, ct)
            ? await Visible(organizationId).SingleOrDefaultAsync(tenant => tenant.Id == id, ct)
            : null;
}
