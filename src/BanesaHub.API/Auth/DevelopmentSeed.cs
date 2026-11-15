using BanesaHub.Application.Organizations;
using BanesaHub.Domain.Organizations;
using BanesaHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.API.Auth;

internal static class DevelopmentSeed
{
    private static readonly (Guid Id, string Email, string Name, string Slug)[] Samples =
    [
        (new("73be0a35-09c4-453b-83df-49e0b237a21a"), "owner-a@example.test", "Prona Demo A", "demo-a"),
        (new("1ebe3e72-70cf-4cd4-a950-fa197bf547b6"), "owner-b@example.test", "Prona Demo B", "demo-b")
    ];

    public static async Task RunAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Seeding is only available in Development.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var database = services.GetRequiredService<BanesaHubDbContext>();
        var users = services.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var registration = services.GetRequiredService<IOrganizationRegistration>();

        await using var transaction = await database.Database.BeginTransactionAsync();
        foreach (var (id, email, name, slug) in Samples)
        {
            var user = await users.FindByNameAsync(email);
            var organization = await database.Organizations.SingleOrDefaultAsync(item => item.Slug == slug);

            if (user?.Id == id && organization is not null &&
                await database.Memberships.AnyAsync(member => member.OrganizationId == organization.Id &&
                    member.UserId == id && member.Role == MembershipRole.Owner && member.IsActive))
            {
                continue;
            }

            if (user is not null || organization is not null)
            {
                throw new InvalidOperationException("Sample account or organization conflicts with existing data.");
            }

            var password = app.Configuration["DevelopmentSeed:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Set DevelopmentSeed:Password in API user secrets before seeding.");
            }

            var created = await users.CreateAsync(new IdentityUser<Guid>
            {
                Id = id,
                UserName = email,
                Email = email
            }, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException("Unable to create the sample account. Check the Identity password policy.");
            }

            if (await registration.CreateAsync(id, name, slug, CancellationToken.None) is null)
            {
                throw new InvalidOperationException("Unable to create the sample organization.");
            }
        }

        await transaction.CommitAsync();
        app.Logger.LogInformation("Development sample organizations and owners are ready.");
    }
}
