using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class RepairOrderConfiguration : IEntityTypeConfiguration<RepairOrder>
{
    public void Configure(EntityTypeBuilder<RepairOrder> builder)
    {
        builder.ToTable("RepairOrders");
        builder.HasKey(o => o.Id);

        // Stored as int, not as a string: the enum has explicit values, and the
        // reporting T-SQL filters on them directly.
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();

        builder.Property(o => o.Description).IsRequired().HasMaxLength(1000);
        builder.Property(o => o.ReceivedAtUtc).IsRequired();

        // TotalPrice is computed from the items — never persisted, so it cannot
        // drift out of sync with them.
        builder.Ignore(o => o.TotalPrice);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.RepairOrder!)
            .HasForeignKey(i => i.RepairOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(RepairOrder.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // The list endpoint filters by status and orders by receipt date.
        builder.HasIndex(o => new { o.Status, o.ReceivedAtUtc });

        // The revenue report filters invoiced orders by invoice date.
        builder.HasIndex(o => new { o.Status, o.InvoicedAtUtc });
    }
}
