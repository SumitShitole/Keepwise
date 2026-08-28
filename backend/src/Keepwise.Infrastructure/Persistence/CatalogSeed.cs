using Keepwise.Domain.Entities;

namespace Keepwise.Infrastructure.Persistence;

public static class CatalogSeed
{
    public static void Ensure(KeepwiseDbContext db)
    {
        if (db.Categories.Any())
        {
            return;
        }

        db.Categories.AddRange(
            Cat("Home appliances", "home-appliances",
                "Refrigerator", "Washing machine", "Air conditioner", "TV", "Water heater", "Microwave", "Dishwasher", "Kitchen appliance"),
            Cat("Electronics", "electronics",
                "Laptop", "Mobile phone", "Tablet", "Smartwatch", "Headphones", "Computer accessory", "Other electronics"),
            Cat("Vehicles", "vehicles", "Car", "Bike", "Scooter", "Other vehicle"),
            Cat("Vehicle related", "vehicle-related",
                "Insurance", "Extended warranty", "Service schedule", "Tyres", "Battery", "Parts", "Accessories"),
            Cat("Other", "other", "AMC", "Subscription", "Insurance", "Service contract", "Warranty", "Other"));

        db.SaveChanges();
    }

    private static Category Cat(string name, string slug, params string[] types) =>
        new()
        {
            Name = name,
            Slug = slug,
            ItemTypes = types.Select(t => new ItemType
            {
                Name = t,
                Slug = t.ToLowerInvariant().Replace(' ', '-')
            }).ToList()
        };
}
