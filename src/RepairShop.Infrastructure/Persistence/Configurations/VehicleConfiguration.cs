using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Entities;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(v => v.Id);

        // The value object is stored as a plain column via a conversion. Going
        // through LicensePlate.Create on read means malformed legacy data fails
        // loudly instead of quietly entering the domain.
        builder.Property(v => v.LicensePlate)
            .HasConversion(
                plate => plate.Value,
                value => LicensePlate.Create(value))
            .HasColumnName("LicensePlate")
            .IsRequired()
            .HasMaxLength(15);

        builder.HasIndex(v => v.LicensePlate).IsUnique();

        builder.Property(v => v.Make).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Model).IsRequired().HasMaxLength(100);

        builder.HasMany(v => v.RepairOrders)
            .WithOne(o => o.Vehicle!)
            .HasForeignKey(o => o.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Vehicle.RepairOrders))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
