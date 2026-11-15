namespace BanesaHub.Web.Inventory;

public enum PropertyFilter
{
    Draft = 1,
    Active = 2
}

public sealed record PropertyRow(Guid Id, string Name, string? Address, string? Category, PropertyFilter Status);

public sealed record PropertyList(
    Guid OrganizationId = default,
    string? Search = null,
    PropertyFilter? Status = null,
    int Page = 1,
    bool HasNext = false,
    IReadOnlyList<PropertyRow>? Rows = null,
    bool SignInRequired = false,
    string? Message = null);
