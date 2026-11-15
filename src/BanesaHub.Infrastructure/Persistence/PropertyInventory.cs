using System.Linq.Expressions;
using BanesaHub.Application.Properties;
using BanesaHub.Domain.Organizations;
using BanesaHub.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class PropertyInventory(BanesaHubDbContext database) : IPropertyInventory
{
    private static readonly Expression<Func<Property, PropertyDetails>> ToDetails =
        property => new PropertyDetails(property.Id, property.Name, property.Address, property.Category, property.Status);

    public async Task<IReadOnlyList<PropertyDetails>?> ListAsync(
        Guid organizationId, Guid actorId, int offset, int limit, CancellationToken ct, string? search = null, PropertyStatus? status = null)
    {
        if (!await CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        var properties = Visible(organizationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            properties = properties.Where(item => item.Name.Contains(term) ||
                (item.Address != null && item.Address.Contains(term)));
        }

        if (status.HasValue)
        {
            properties = properties.Where(item => item.Status == status.Value);
        }

        return await properties.OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Skip(offset).Take(limit).Select(ToDetails).ToListAsync(ct);
    }

    public async Task<PropertyDetails?> FindAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct)
    {
        if (!await CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        return await Visible(organizationId).Where(item => item.Id == id).Select(ToDetails).SingleOrDefaultAsync(ct);
    }

    public async Task<PropertyDetails?> CreateAsync(
        Guid organizationId, Guid actorId, PropertyInput input, CancellationToken ct)
    {
        if (!await CanManage(database, organizationId, actorId, ct))
        {
            return null;
        }

        var property = new Property
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = input.Name,
            Address = input.Address,
            Category = input.Category
        };
        database.Properties.Add(property);
        await database.SaveChangesAsync(ct);
        return new PropertyDetails(property.Id, property.Name, property.Address, property.Category, property.Status);
    }

    public async Task<bool> UpdateAsync(
        Guid organizationId, Guid actorId, Guid id, PropertyInput input, CancellationToken ct)
    {
        var property = await FindEditable(organizationId, actorId, id, ct);
        if (property is null)
        {
            return false;
        }

        property.Name = input.Name;
        property.Address = input.Address;
        property.Category = input.Category;
        await database.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid organizationId, Guid actorId, Guid id, CancellationToken ct)
    {
        var property = await FindEditable(organizationId, actorId, id, ct);
        if (property is null)
        {
            return false;
        }

        property.Status = PropertyStatus.Archived;
        await database.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Property> Visible(Guid organizationId) =>
        database.Properties.Where(item => item.OrganizationId == organizationId && item.Status != PropertyStatus.Archived);

    private async Task<Property?> FindEditable(Guid organizationId, Guid actorId, Guid id, CancellationToken ct) =>
        await CanManage(database, organizationId, actorId, ct)
            ? await Visible(organizationId).SingleOrDefaultAsync(item => item.Id == id, ct)
            : null;

    internal static Task<bool> CanManage(BanesaHubDbContext database, Guid organizationId, Guid actorId, CancellationToken ct) =>
        OrganizationDirectory.ActiveMemberships(database, actorId).AnyAsync(member =>
            member.OrganizationId == organizationId &&
            (member.Role == MembershipRole.Owner || member.Role == MembershipRole.PropertyManager) &&
            database.Organizations.Any(org => org.Id == organizationId && org.IsActive), ct);
}
