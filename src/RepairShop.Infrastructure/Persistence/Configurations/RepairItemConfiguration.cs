using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairShop.Domain.Entities;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class RepairItemConfiguration : IEntityTypeConfiguration<RepairItem>
{
    public void Configure(EntityTypeBuilder<RepairItem> builder)
    {
        builder.ToTable("RepairItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Type).HasConversion<int>().IsRequired();
        builder.Property(i => i.Designation).IsRequired().HasMaxLength(200);

        // decimal(18,2) for money and (10,2) for quantity: the SQL Server default
        // of decimal(18,2) is fine for prices, but leaving it implicit produces a
        // truncation warning on every model build.
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Quantity).HasColumnType("decimal(10,2)");

        builder.Ignore(i => i.LineTotal);
    }
}
