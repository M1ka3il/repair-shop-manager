using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping lives here rather than as attributes on the entity — the domain type
/// stays free of persistence concerns, which is the whole point of the layering.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);

        builder.HasMany(c => c.Vehicles)
            .WithOne(v => v.Customer!)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Customer.Vehicles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
