using GermanyDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyDashboard.Infrastructure.Persistence.Configurations;

public class DataPointConfiguration : IEntityTypeConfiguration<DataPoint>
{
    public void Configure(EntityTypeBuilder<DataPoint> builder)
    {
        builder.Property(d => d.Value).HasPrecision(18, 4);

        builder.HasIndex(d => new { d.IndicatorId, d.RegionId, d.Year }).IsUnique();

        builder.HasOne(d => d.Indicator)
            .WithMany(i => i.DataPoints)
            .HasForeignKey(d => d.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Region)
            .WithMany(r => r.DataPoints)
            .HasForeignKey(d => d.RegionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
