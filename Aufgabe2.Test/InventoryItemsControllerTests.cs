using BaseModel.Infrastructure;
using Eventmanager.Test;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aufgabe2.Test;

public class InventoryItemsControllerTests
{
    private readonly TestWebApplicationFactory<ReservationContext> _factory;
    private readonly SqliteConnection _connection = new SqliteConnection("DataSource=:memory:");
    public InventoryItemsControllerTests()
    {
        _factory = new TestWebApplicationFactory<ReservationContext>();
        _factory.SubstituteService<ReservationContext>(opt => opt.UseSqlite(_connection));
    }
    private void GenerateFixtures()
    {
        _factory.InitializeDatabase(db =>
        {
            // TODO: Add your database fixtures.
            db.SaveChanges();
        });
    }

    // TODO: Add your integration tests
}
