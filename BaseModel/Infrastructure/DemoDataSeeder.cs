using BaseModel.Model;
using Bogus;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Person = BaseModel.Model.Person;

namespace BaseModel.Infrastructure;

/// <summary>
/// Fills an empty database with a small, believable school inventory so the app has something
/// to show right after the first start. The data is deterministic (fixed random seed) and laid
/// out relative to <c>today</c>, so there are always a few open and a few overdue loans.
/// </summary>
public static class DemoDataSeeder
{
    private static readonly (string Category, string Prefix, string[] Items)[] Catalog =
    [
        ("Notebooks", "NB", ["Lenovo ThinkPad T14", "Dell Latitude 5440", "HP EliteBook 840 G10", "MacBook Air 13\" M2"]),
        ("Adapters", "AD", ["USB-C to HDMI adapter", "USB-C docking station", "DisplayPort to HDMI adapter", "USB-C to Ethernet adapter"]),
        ("Cables", "CB", ["HDMI cable 2 m", "USB-C charging cable 1 m", "DisplayPort cable 3 m", "Network cable Cat6 5 m"]),
        ("Projectors", "PJ", ["Epson EB-W49 projector", "BenQ MW560 projector"]),
    ];

    private static readonly string[] SchoolClasses = ["5AAIF", "5BAIF", "5CAIF", "8ABIF", "8ACIF", "5AKIF", "5BKIF"];

    /// <summary>Seeds the demo data unless the database already contains inventory items.</summary>
    /// <returns><c>true</c> when data was written.</returns>
    public static async Task<bool> SeedIfEmptyAsync(
        ReservationContext db, DateOnly today, CancellationToken cancellationToken = default)
    {
        if (await db.InventoryItems.AnyAsync(cancellationToken))
            return false;

        var faker = new Faker("de") { Random = new Randomizer(1938) };

        var staff = Enumerable.Range(0, 3)
            .Select(_ => new Staff(faker.Name.FirstName(), faker.Name.LastName()))
            .ToList();
        var persons = Enumerable.Range(0, 20).Select(_ => CreatePerson(faker)).ToList();

        var items = new List<InventoryItem>();
        foreach (var (categoryName, prefix, itemNames) in Catalog)
        {
            var category = new Category(categoryName);
            items.AddRange(itemNames.Select(name => new InventoryItem(
                $"{prefix}-{faker.Random.String2(6, "0123456789ABCDEF")}", name, category, isAvailable: true)));
        }

        var reservations = new List<Reservation>();
        for (var index = 0; index < items.Count; index++)
            reservations.AddRange(CreateLoans(faker, items[index], index, persons, staff, today));

        db.AddRange(staff);
        db.AddRange(persons);
        db.AddRange(items);
        db.AddRange(reservations);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Person CreatePerson(Faker faker)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        return new Person(
            firstName, lastName, faker.Random.ListItem(SchoolClasses),
            // example.com is reserved for documentation, so no real mailbox is ever shown.
            faker.Internet.Email(firstName, lastName, "example.com").ToLowerInvariant(),
            faker.Phone.PhoneNumber("+43 6## #######"));
    }

    /// <summary>
    /// Builds the loan history of one item: up to three closed loans in the past and, for every
    /// third item, one loan that is still open. Loans of an item never overlap.
    /// </summary>
    private static IEnumerable<Reservation> CreateLoans(
        Faker faker, InventoryItem item, int index, List<Person> persons, List<Staff> staff, DateOnly today)
    {
        // Every fifth item has no closed loans. Unless it is on loan right now, it shows up in the
        // "never lent" report.
        var closedLoans = index % 5 == 4 ? 0 : faker.Random.Int(1, 3);
        var hasOpenLoan = index % 3 == 0;
        // One item came back damaged and is still blocked.
        var lastReturnWasDamaged = index == 7;

        var cursor = today.AddDays(-faker.Random.Int(28, 40));
        for (var loan = 0; loan < closedLoans; loan++)
        {
            var returnDate = cursor;
            var loanDate = returnDate.AddDays(-faker.Random.Int(1, 10));
            var reservation = new Reservation(faker.Random.ListItem(persons), item, loanDate);
            reservation.Return(returnDate, faker.Random.ListItem(staff), damaged: lastReturnWasDamaged && loan == 0);
            yield return reservation;

            cursor = loanDate.AddDays(-faker.Random.Int(3, 20));
        }

        if (hasOpenLoan)
        {
            // Between 1 and 24 days ago, so some of the open loans are already overdue.
            yield return new Reservation(
                faker.Random.ListItem(persons), item, today.AddDays(-faker.Random.Int(1, 24)));
        }

        item.IsAvailable = !hasOpenLoan && !lastReturnWasDamaged;
    }
}
