using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class BanesaHubDbContext(DbContextOptions<BanesaHubDbContext> options) : DbContext(options)
{
}
