using BaseModel.Model;
using Microsoft.EntityFrameworkCore;

namespace BaseModel.Infrastructure;

public class ReservationContext(DbContextOptions<ReservationContext> options) : DbContext(options)
{
    /// <summary>Name of the index that allows at most one open loan per inventory item.</summary>
    public const string OpenLoanIndexName = "IX_reservation_openLoanPerItem";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(item => item.SerialNumber)
            .IsUnique();

        modelBuilder.Entity<Reservation>(reservation =>
        {
            reservation.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Closing a loan only succeeds while it is still open in the database. Two users
            // returning the same item at once cannot both win.
            reservation.Property(r => r.ReturnDate).IsConcurrencyToken();

            reservation.HasOne(r => r.InventoryItem)
                .WithMany(item => item.Reservations)
                .HasForeignKey("InventoryItemId");

            // The database itself guarantees that an item cannot be handed out twice, even if
            // two users lend it at the same moment: only one row per item may have no return date.
            reservation.HasIndex(["InventoryItemId"], OpenLoanIndexName)
                .IsUnique()
                .HasFilter("\"returnDate\" IS NULL");

            // The partial index above only covers open loans; the history of an item needs
            // an index over all of its loans.
            reservation.HasIndex("InventoryItemId");
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Table names follow the entity name (not the DbSet) and start lower-case.
            var clrName = entityType.ClrType.Name;
            entityType.SetTableName(char.ToLowerInvariant(clrName[0]) + clrName[1..]);

            foreach (var property in entityType.GetProperties())
            {
                var propName = property.Name;
                property.SetColumnName(char.ToLowerInvariant(propName[0]) + propName[1..]);

                if (property.ClrType == typeof(string) && property.GetMaxLength() is null)
                    property.SetMaxLength(255);
            }

            // Nothing is deleted implicitly: a person or item with loans cannot simply disappear.
            foreach (var fk in entityType.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
