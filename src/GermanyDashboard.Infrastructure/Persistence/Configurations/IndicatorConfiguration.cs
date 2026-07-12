using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyDashboard.Infrastructure.Persistence.Configurations;

public class IndicatorConfiguration : IEntityTypeConfiguration<Indicator>
{
    public void Configure(EntityTypeBuilder<Indicator> builder)
    {
        builder.HasIndex(i => i.Slug).IsUnique();
        builder.Property(i => i.Slug).HasMaxLength(96);
        builder.Property(i => i.Name).HasMaxLength(160);
        builder.Property(i => i.Unit).HasMaxLength(32);
        builder.Property(i => i.ValueFormat).HasMaxLength(32);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Indicators)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.DataSource)
            .WithMany(s => s.Indicators)
            .HasForeignKey(i => i.DataSourceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
