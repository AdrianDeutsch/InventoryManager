using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseModel.Infrastructure;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling to create migrations. It never touches a real
/// database, so the connection string is a placeholder.
/// </summary>
public class DesignTimeContextFactory : IDesignTimeDbContextFactory<ReservationContext>
{
    public ReservationContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<ReservationContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options);
}
